import path = require("path");
import tl = require('vsts-task-lib/task');

export class CertesAuthzTask {
    
    public static checkIfAzurePythonSdkIsInstalled() {
        return !!tl.which("az", true);
    }

    public static async runMain() {
        try {
            tl.setResourcePath(path.join(__dirname, "task.json"));

            let indentifiers: string = tl.getInput('indentifiers', true);
            tl.debug('indentifiers:' +indentifiers);
        } catch (err) {
            tl.setResult(tl.TaskResult.Failed, err.message);
        }
    }
}

if (!CertesAuthzTask.checkIfAzurePythonSdkIsInstalled()) {
    tl.setResult(tl.TaskResult.Failed, tl.loc("AzureSDKNotFound"));
} else {
    CertesAuthzTask.runMain();
}
