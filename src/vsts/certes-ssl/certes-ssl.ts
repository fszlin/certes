import path = require("path");
import tl = require('vsts-task-lib/task');

export class CertesAuthzTask {
    
    public static checkIfAzurePythonSdkIsInstalled() {
        return !!tl.which("az", true);
    }

    public static async runMain() {
        try {
            tl.setResourcePath(path.join(__dirname, "task.json"));

            const indentifiers = tl.getDelimitedInput('identifiers', '\n', true);
            
            tl.debug('indentifiers:');
            indentifiers.forEach(v => tl.debug(v));

            tl.setResult(tl.TaskResult.Succeeded, tl.loc("ScriptReturnCode", 0));
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
