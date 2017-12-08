
import * as ma from 'vsts-task-lib/mock-answer';
import * as tmrm from 'vsts-task-lib/mock-run';
import * as path from 'path';
import { MockHelper } from './../common/MockHelper';

const endpointId = 'acme-account-id';
const taskPath = path.join(__dirname, '../../../', 'src/vsts/', 'authorization/AuthorizationTask.ts');
const tmr: tmrm.TaskMockRunner = new tmrm.TaskMockRunner(taskPath);

const mtt = require('vsts-task-lib/mock-toolrunner');
tmr.registerMock('vsts-task-lib/toolrunner', mtt);

tmr.setInput('acmeAccount', endpointId);
tmr.setInput('identifiers', 'www.example.com\nweb.example.com\nnews.example.com\n');

MockHelper.mockServiceEndpoint(endpointId, {
    directoryUri: 'https://example.com/',
    certificate: 'cert-data'
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
        '/mocked/tools/dotnet acme': {
            'code': 0,
            'stdout': 'acme called',
        }
    }
};

tmr.setAnswers(a);

tmr.run();
