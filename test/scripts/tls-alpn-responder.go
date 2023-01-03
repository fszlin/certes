// based on https://github.com/jefferai/golang-alpn-example/blob/master/alpnexample.go

// scp ./tls-alpn-responder.go elin@certes-alpn.canadacentral.cloudapp.azure.com:/home/elin/tls-alpn/tls-alpn-responder.go

// sudo apt install golang-go
// go mod init certes.app/tls-apln
// go get .

// nohup sudo go run tls-alpn-responder.go > /dev/null 2>&1

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

type alpnCertStruct struct {
	Cert []byte
	Key  []byte
}

var (
	wg                    = &sync.WaitGroup{}
	shutdownCh            = make(chan struct{})
	selfSignedCertificate *tls.Certificate
	acmeCertificates      = make(map[string]*tls.Certificate)
)

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
	subjectName := r.Host
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

func runServer(tlsConfig *tls.Config) {
	defer wg.Done()

	mux := http.NewServeMux()
	mux.HandleFunc("/tls-alpn-01/", setupAlpn)

	ln, err := net.Listen("tcp", ":443")
	if err != nil {
		fmt.Printf("error starting listener: %v\n", err)
		os.Exit(1)
	}
	tlsLn := tls.NewListener(ln, tlsConfig)

	server := &http.Server{
		Addr:    ":443",
		Handler: mux,
		TLSNextProto: map[string]func(*http.Server, *tls.Conn, http.Handler){
			"acme-tls/1": handleAcmeRequest,
		},
	}

	http2.ConfigureServer(server, nil)

	go func() {
		err = server.Serve(tlsLn)
		fmt.Printf("server returned: %v\n", err)
	}()

	select {
	case <-shutdownCh:
		return
	}
}

func handleAcmeRequest(server *http.Server, conn *tls.Conn, handler http.Handler) {
	fmt.Printf("acme-tls/1 handshake complete\n")
}

func makeDefaultCertificate() *x509.Certificate {
	key, err := ecdsa.GenerateKey(elliptic.P256(), rand.Reader)
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

	certBytes, err := x509.CreateCertificate(rand.Reader, template, template, key.Public(), key)
	if err != nil {
		fmt.Printf("error generating self-signed cert: %v\n", err)
		os.Exit(1)
	}

	cert, err := x509.ParseCertificate(certBytes)
	if err != nil {
		fmt.Printf("error parsing generated certificate: %v\n", err)
		os.Exit(1)
	}

	selfSignedCertificate = &tls.Certificate{
		Certificate: [][]byte{certBytes},
		PrivateKey:  key,
	}

	return cert
}

func main() {
	cert := makeDefaultCertificate()

	certPool := x509.NewCertPool()
	certPool.AddCert(cert)
	tlsConfig := &tls.Config{
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
	go runServer(tlsConfig)
	wg.Wait()
}
