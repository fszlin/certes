import tl = require('vsts-task-lib/task');

export class AzCommand {
    
    public static checkIfAzurePythonSdkIsInstalled() {
        return !!tl.which("az", true);
    }
}
