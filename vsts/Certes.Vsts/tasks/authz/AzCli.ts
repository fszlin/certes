import tl = require('vsts-task-lib/task');

export interface IDnsRecordSet {
    etag: string;
    fqdn: string;
    id: string;
    metadata: any;
    name: string;
    resourceGroup: string;
    ttl: number;
    type: string;
}

export class AzCli {
    private static loggedInService: string = null;
    
    public static checkIfAzurePythonSdkIsInstalled() {
        return !!tl.which('az', true);
    }

    public static loginAzure(serviceName: string) {
        var connectedService: string = tl.getInput(serviceName, true);
        if (connectedService !== this.loggedInService) {
            this.loginAzureRM(connectedService);
        }
    }

    private static loginAzureRM(connectedService: string): void {
        var servicePrincipalId: string = tl.getEndpointAuthorizationParameter(connectedService, 'serviceprincipalid', false);
        var servicePrincipalKey: string = tl.getEndpointAuthorizationParameter(connectedService, 'serviceprincipalkey', false);
        var tenantId: string = tl.getEndpointAuthorizationParameter(connectedService, 'tenantid', false);
        var subscriptionName: string = tl.getEndpointDataParameter(connectedService, 'SubscriptionName', true);
        //login using svn
        this.throwIfError(
            tl.execSync('az', `login --service-principal -u "${servicePrincipalId}" -p "${servicePrincipalKey}" --tenant "${tenantId}"`));
        this.loggedInService = connectedService;
        //set the subscription imported to the current subscription
        this.throwIfError(tl.execSync('az', `account set --subscription "${subscriptionName}"`));
    }

    private static throwIfError(resultOfToolExecution): void {
        if (resultOfToolExecution.stderr) {
            throw resultOfToolExecution;
        }
    }
}
