import tl = require('vsts-task-lib/task');

export class AzCli {
    private static isLoggedIn: boolean = false;
    
    public static checkIfAzurePythonSdkIsInstalled() {
        return !!tl.which("az", true);
    }

    private static loginAzure() {
        var connectedService: string = tl.getInput("connectedServiceNameARM", true);
        this.loginAzureRM(connectedService);
    }

    private static loginAzureRM(connectedService: string): void {
        var servicePrincipalId: string = tl.getEndpointAuthorizationParameter(connectedService, "serviceprincipalid", false);
        var servicePrincipalKey: string = tl.getEndpointAuthorizationParameter(connectedService, "serviceprincipalkey", false);
        var tenantId: string = tl.getEndpointAuthorizationParameter(connectedService, "tenantid", false);
        var subscriptionName: string = tl.getEndpointDataParameter(connectedService, "SubscriptionName", true);
        //login using svn
        this.throwIfError(tl.execSync("az", "login --service-principal -u \"" + servicePrincipalId + "\" -p \"" + servicePrincipalKey + "\" --tenant \"" + tenantId + "\""));
        this.isLoggedIn = true;
        //set the subscription imported to the current subscription
        this.throwIfError(tl.execSync("az", "account set --subscription \"" + subscriptionName + "\""));
    }

    private static throwIfError(resultOfToolExecution): void {
        if (resultOfToolExecution.stderr) {
            throw resultOfToolExecution;
        }
    }
}
