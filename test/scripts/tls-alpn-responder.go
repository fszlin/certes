// based on https://github.com/jefferai/golang-alpn-example/blob/master/alpnexample.go

package main

import (
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/tls"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/json"
	"fmt"
	"math/big"
	"net"
	"net/http"
	"os"
	"os/signal"
	"sync"
	"syscall"
	"time"

	"golang.org/x/net/http2"
)

// This program demonstrates using ALPN with TLS to handle both HTTP/2 traffic
// and an arbitrary protocol called "foo". Although this method of multiplexing
// requires multiple TCP connections, the number is few (since HTTP/2 reuses
// connections and multiplexes and yamux can be layered on top of a given
// net.Conn), and the same port can be used because the protocol switch happens
// during TLS negotiation.
//
// The sleep times are distinct between the foo and HTTP/2 clients to make it
// clear that they are running concurrently/async.

var (
	wg          = &sync.WaitGroup{}
	certBytes   []byte
	cert        *x509.Certificate
	key         *ecdsa.PrivateKey
	shutdownCh  = make(chan struct{})
	h2TLSConfig *tls.Config
	certPool    = x509.NewCertPool()

	selfSignedCertificate *tls.Certificate
	acmeCertificates      = make(map[string]*tls.Certificate)
)

type alpnCertStruct struct {
	Cert []byte
	Key  []byte
}

func getAcmeCertificate(clientHello *tls.ClientHelloInfo) (*tls.Certificate, error) {
	fmt.Fprintf(os.Stdout, "getAcmeCertificate %s\n", clientHello.ServerName)
	for _, p := range clientHello.SupportedProtos {
		if p == "acme-tls/1" {
			return acmeCertificates[clientHello.ServerName], nil
		}
	}

	return selfSignedCertificate, nil
}

func setupAlpn(w http.ResponseWriter, r *http.Request) {
	subjectName := r.URL.Path[len("/tls-alpn-01/"):]
	fmt.Printf("setup tls-alpn-01 for %s\n", subjectName)

	decoder := json.NewDecoder(r.Body)
	var t alpnCertStruct
	decoder.Decode(&t)
	cert, err := x509.ParseCertificate(t.Cert)
	if err != nil {
		fmt.Printf("parsex509: %s", err)
		return
	}

	key, err := x509.ParsePKCS8PrivateKey(t.Key)
	if err != nil {
		fmt.Fprintf(w, "parsekey: %s", err)
		return
	}

	tlsCert := tls.Certificate{
		Certificate: [][]byte{cert.Raw},
		PrivateKey:  key,
	}

	acmeCertificates[subjectName] = &tlsCert

	fmt.Fprintf(w, subjectName)
}

// runServer runs a TLS connection that indicates support for HTTP/2 and for
// arbitrary protocol "foo" negotiated via TLS ALPN. The server contains a
// mapping of this protocol name to handleFoo. For demonstrating HTTP request
// handling, it implements a route that simply returns "gotcha" back to the
// client.
func runServer() {
	defer wg.Done()

	mux := http.NewServeMux()
	mux.HandleFunc("/tls-alpn-01/", setupAlpn)

	ln, err := net.Listen("tcp", ":443")
	if err != nil {
		fmt.Printf("error starting listener: %v\n", err)
		os.Exit(1)
	}
	tlsLn := tls.NewListener(ln, h2TLSConfig)

	server := &http.Server{
		Addr:    ":443",
		Handler: mux,
		TLSNextProto: map[string]func(*http.Server, *tls.Conn, http.Handler){
			"acme-tls/1": handleFoo,
		},
	}

	http2.ConfigureServer(server, nil)

	go func() {
		err = server.Serve(tlsLn)
		fmt.Printf("server returned: %v\n", err)
	}()

	//fmt.Printf("server nextprotos: %#v\n", server.TLSNextProto)

	select {
	case <-shutdownCh:
		return
	}
}

// handleFoo is started by the selection of "foo" as the protocol, which the
// foo client advertises as its only supported protocol. The function is given
// a raw tls.Conn, over which we can do anything we like -- this is a simple
// echo server with read/write deadlines.
func handleFoo(server *http.Server, conn *tls.Conn, handler http.Handler) {
	fmt.Printf("acme-tls/1 handshake complete\n")
}

// main generates a shared, self-signed certificate, sets up the mostly-shared
// TLS configuration, adds Ctrl-C handling for easy exit, and then starts the
// various other functions running, waiting for them to finish after a Ctrl-C.
func main() {
	var err error
	key, err = ecdsa.GenerateKey(elliptic.P256(), rand.Reader)
	if err != nil {
		fmt.Printf("error generating key: %v\n", err)
		os.Exit(1)
	}

	template := &x509.Certificate{
		Subject: pkix.Name{
			CommonName: "localhost",
		},
		DNSNames: []string{"localhost"},
		ExtKeyUsage: []x509.ExtKeyUsage{
			x509.ExtKeyUsageServerAuth,
			x509.ExtKeyUsageClientAuth,
		},
		KeyUsage:              x509.KeyUsageDigitalSignature | x509.KeyUsageKeyEncipherment | x509.KeyUsageKeyAgreement | x509.KeyUsageCertSign,
		SerialNumber:          big.NewInt(1),
		NotBefore:             time.Now().Add(-1 * time.Second),
		NotAfter:              time.Now().Add(24 * 365 * time.Hour),
		BasicConstraintsValid: true,
		IsCA: true,
	}

	certBytes, err = x509.CreateCertificate(rand.Reader, template, template, key.Public(), key)
	if err != nil {
		fmt.Printf("error generating self-signed cert: %v\n", err)
		os.Exit(1)
	}

	cert, err = x509.ParseCertificate(certBytes)
	if err != nil {
		fmt.Printf("error parsing generated certificate: %v\n", err)
		os.Exit(1)
	}

	certPool.AddCert(cert)

	selfSignedCertificate = &tls.Certificate{
		Certificate: [][]byte{certBytes},
		PrivateKey:  key,
	}

	h2TLSConfig = &tls.Config{
		GetCertificate: getAcmeCertificate,
		RootCAs:        certPool,
		ClientAuth:     tls.NoClientCert,
		ClientCAs:      certPool,
		NextProtos: []string{
			"acme-tls/1",
		},
	}

	sighupCh := make(chan os.Signal, 4)
	signal.Notify(sighupCh, os.Interrupt, syscall.SIGTERM)
	go func() {
		<-sighupCh
		close(shutdownCh)
	}()

	wg.Add(1)
	go runServer()
	wg.Wait()
}
