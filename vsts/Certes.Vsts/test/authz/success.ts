
import * as ma from 'vsts-task-lib/mock-answer';
import * as tmrm from 'vsts-task-lib/mock-run';
import * as path from 'path';
import { MockHelper } from './../MockHelper';

const endpointId = 'acme-account-id';
const azEndpointId = 'az-endpoint-id';
const taskPath = path.join(__dirname, '../../tasks', 'authz/AuthzTask.ts');
const tmr: tmrm.TaskMockRunner = new tmrm.TaskMockRunner(taskPath);

const mtt = require('vsts-task-lib/mock-toolrunner');
tmr.registerMock('vsts-task-lib/toolrunner', mtt);

tmr.setInput('acmeAccount', endpointId);
tmr.setInput('identifiers', 'www.example.com\nweb.example.com\nnews.example.com\n');
tmr.setInput('frameworkVersion', 'netcoreapp2.0');
tmr.setInput('cliVersion', '1.1.4');

tmr.setInput('azureDnsAccount', azEndpointId);

MockHelper.mockServiceEndpoint(
    endpointId, {
        directoryUri: 'https://example.com/',
        certificate: 'cert-data'
    }, null);

MockHelper.mockServiceEndpoint(
    azEndpointId, {
        serviceprincipalid: 'serviceprincipalid',
        serviceprincipalkey: 'serviceprincipalkey',
        tenantid: 'tenantid'
    }, {
        SubscriptionName: 'SubscriptionName'
    });

let a: ma.TaskLibAnswers = <ma.TaskLibAnswers>{
    'which': {
        'az': '/mocked/tools/az',
        'dotnet': '/mocked/tools/dotnet',
    },
    'checkPath': {
        '/mocked/tools/az': true,
        '/mocked/tools/dotnet': true,
    },
    'exec': {
        '/mocked/tools/az': {
            'code': 0,
            'stdout': '',
        },
        '/mocked/tools/dotnet restore': {
            'code': 0,
            'stdout': 'restore complete',
        },
        '/mocked/tools/dotnet acme import --key-file key.pem': {
            'code': 0,
            'stdout': 'account key imported',
        },
        '/mocked/tools/dotnet acme authz --server https://example.com/ --v www.example.com --v web.example.com --v news.example.com': {
            'code': 0,
            'stdout': 'acme called'
        },
        '/mocked/tools/az login --service-principal -u "serviceprincipalid" -p "serviceprincipalkey" --tenant "tenantid"': {
            code: 0,
        },
        '/mocked/tools/az account set --subscription "SubscriptionName"': {
            code: 0,
        },
        "/mocked/tools/az resource list --query [?name=='example.com'] && [?type=='Microsoft.Network/dnszones']": {
            code: 0,
            stdout: JSON.stringify([
                {
                    "name": "example.com",
                    "resourceGroup": "azureDnsResourceGroup",
                    "type": "Microsoft.Network/dnszones"
                }
            ])
        },
        "/mocked/tools/az network dns record-set txt list -g azureDnsResourceGroup -z example.com --query [?name=='_acme-challenge.www']": {
            code: 0,
            stdout: '[]'
        },
        "/mocked/tools/az network dns record-set txt list -g azureDnsResourceGroup -z example.com --query [?name=='_acme-challenge.web']": {
            code: 0,
            stdout: '[]'
        },
        "/mocked/tools/az network dns record-set txt list -g azureDnsResourceGroup -z example.com --query [?name=='_acme-challenge.news']": {
            code: 0,
            stdout: '[]'
        }
    }
};

tmr.setAnswers(a);

tmr.run();
