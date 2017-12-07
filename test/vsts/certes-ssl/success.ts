
import ma = require('vsts-task-lib/mock-answer');
import tmrm = require('vsts-task-lib/mock-run');
import path = require('path');

let taskPath = path.join(__dirname, '../../../', 'src/vsts/certes-ssl/certes-ssl.ts');
let tmr: tmrm.TaskMockRunner = new tmrm.TaskMockRunner(taskPath);

tmr.setInput('identifiers', "www.example.com\nweb.example.com\nnews.example.com\n");

let a: ma.TaskLibAnswers = <ma.TaskLibAnswers>{
    "which": {
        "az": "/mocked/tools/az",
    },
    "checkPath": {
        "/mocked/tools/az": true,
    },
    "exec": {
        "/mocked/tools/az": {
            "code": 0,
            "stdout": "",
        }
    }
};

tmr.setAnswers(a);

tmr.run();
