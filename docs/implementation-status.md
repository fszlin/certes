# Implementation Status

This document details the ACME resources and flows implemented by *Certes*.

| Resource      | Action                    | Spec [draft-07][spec-07] | Boulder | Certes |
| :---          | :---                      | :---: | :---: | :---: |
| Directory     | get                       | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |
| Nonce         | new                       | :heavy_check_mark: | | |
|               | parse from header         | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |
| Account       | new                       | :heavy_check_mark: | | |
|               | finding URL by key        | :heavy_check_mark: | | |
|               | update                    | :heavy_check_mark: | | |
|               | change of ToS             | :heavy_check_mark: | | |
|               | ext. account binding      | :heavy_check_mark: | | |
|               | key roll-over             | :heavy_check_mark: | | |
|               | deactivation              | :heavy_check_mark: | | |
| Registration  | new                       | :heavy_multiplication_x: | :heavy_check_mark: [draft-02][spec-02-reg] | :heavy_check_mark: [draft-02][spec-02-reg] |
|               | update                    | :heavy_multiplication_x: | :heavy_check_mark: [draft-02][spec-02-reg] | :heavy_check_mark: [draft-02][spec-02-reg] |
|               | key roll-over             | :heavy_multiplication_x: | :heavy_check_mark: [draft-05][spec-05-key-change] | |
|               | delete                    | :heavy_multiplication_x: | :heavy_check_mark: | |
| Authorization | new                       | :heavy_check_mark: | :heavy_check_mark: [draft-02][spec-02-authz] | :heavy_check_mark: [draft-02][spec-02-authz] |
|               | responding to challenges  | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |
|               | check status              | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |
|               | deactivation              | :heavy_check_mark: | | |
|               | delete                    | :heavy_multiplication_x: | :heavy_check_mark: | |
| Order         | new                       | :heavy_check_mark: | | | |
| Certificate   | new                       | :heavy_multiplication_x: | :heavy_check_mark: [draft-02][spec-02-cert] | :heavy_check_mark: [draft-02][spec-02-cert] |
|               | download                  | :heavy_check_mark: | :heavy_check_mark: [draft-02][spec-02-cert] | :heavy_check_mark: [draft-02][spec-02-cert] |
|               | revoke                    | :heavy_multiplication_x: | :heavy_check_mark: [draft-02][spec-02-cert-revoke] | :heavy_check_mark: [draft-02][spec-02-cert-revoke] |


[spec-02-reg]: https://tools.ietf.org/html/draft-ietf-acme-acme-02#section-6.3
[spec-02-authz]: https://tools.ietf.org/html/draft-ietf-acme-acme-02#section-6.4
[spec-02-cert]: https://tools.ietf.org/html/draft-ietf-acme-acme-02#section-6.5
[spec-02-cert-revoke]: https://tools.ietf.org/html/draft-ietf-acme-acme-02#section-6.6
[spec-05-key-change]: https://tools.ietf.org/html/draft-ietf-acme-acme-05#section-6.3.3
[spec-07]: https://tools.ietf.org/html/draft-ietf-acme-acme-07
