import * as path from 'path';
import * as tl from 'vsts-task-lib/task';
import * as st from 'string-template';
import * as az from './AzCli';

const TempFolder = '.certes';
const ProjContentTemplate = `
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{frameworkVersion}</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <DotNetCliToolReference Include="Certes.Cli" Version="{cliVersion}" />
  </ItemGroup>
</Project>
`;

export class AuthorizationTask {

    private nugetSource: string;
    private acmeAccount: string;
    private indentifiers: string[];
    private directoryUri: string;
    private prviKey: string;
    private cliVersion: string;
    private frameworkVersion: string;

    constructor() {
        this.acmeAccount = tl.getInput('acmeAccount', true);
        this.indentifiers = tl.getDelimitedInput('identifiers', '\n', true);
        this.nugetSource = tl.getInput('nugetSource', false);
        this.frameworkVersion = tl.getInput('frameworkVersion', false);
        this.cliVersion = tl.getInput('cliVersion', true);

        this.directoryUri = tl.getEndpointAuthorizationParameter(this.acmeAccount, 'directoryUri', false);
        this.prviKey = tl.getEndpointAuthorizationParameter(this.acmeAccount, 'certificate', false);
    }

    public async execute() {
        tl.setResourcePath(path.join(__dirname, 'task.json'));

        tl.debug('indentifiers to authz:');
        this.indentifiers.forEach(v => tl.debug(v));

        tl.mkdirP(`./${TempFolder}`);

        try {
            tl.pushd(`./${TempFolder}`);

            // minimal project file for restoring CLI as cli-tool
            const projContent = st(ProjContentTemplate, {
                frameworkVersion: this.frameworkVersion,
                cliVersion: this.cliVersion,
            });

            tl.debug(projContent);
            tl.writeFile('certes.csproj', projContent);
            
            // call dotnet restore
            let args = ['restore'];
            if (this.nugetSource) {
                args = args.concat(['-s', this.nugetSource]);
            }

            tl.execSync('dotnet', args);

            tl.writeFile('key.pem', this.prviKey);
            tl.execSync('dotnet', ['acme', 'import', '--key-file', 'key.pem']);

            args = ['acme', 'authz', '--server', this.directoryUri];
            this.indentifiers.forEach(v => args = args.concat(['--v', v]));
            tl.execSync('dotnet', args);

            //az.AzCli.loginAzure('azureDnsAccount');

            tl.setResult(tl.TaskResult.Succeeded, tl.loc('ScriptReturnCode', 0));
        }
        finally {
            tl.popd();
        }
    }
}

var tsk = new AuthorizationTask();
tsk.execute().catch((reason) => tl.setResult(tl.TaskResult.Failed, reason));
