
# Design

This document details the API exposed for handling ACME flows, as of [draft-07][draft-07].


## Initialization

Creating the context with specific ACME server by providing the directory URI.
```C#
var context = new AcmeContext(dirUri);
```

## Acounts

Creating or fetching account.
```C#
var account = await context.Account(new []{ "mailto:certes@example.com" });
var account = await context.Account(key);
```

Accepting terms of service.
```C#
var tosUri = await account.AcceptTermsOfService();
var accountInfo = await account.Resource();
```

Updating account contact.
```C#
await account.Update(accountInfo);
```

Accepting terms of service.
```C#
await account.AcceptTermsOfService();
```

Changing account key.
```C#
await account.ChangeKey(key);
```

Deactivation.
```C#
await account.Deactivate();
```

Navigating to related entities.
```C#
var orders = await account.Orders();
```

## Orders

Applying for Certificate Issuance
```C#
var order = await orders.New(orderInfo);
```

## Authorizations

```C#
var authorizations = await order.Authorizations();
```

Creating new authorization for identifier.
```C#
var authz = await authorizations.New(identifier);
```

## Challenges

```C#
var challenges = await authz.Challenges();
```


## Not Implemented
* Account
  * External Account Binding
 
[draft-07]: https://tools.ietf.org/html/draft-ietf-acme-acme-07
