import path = require("path");
import tl = require('vsts-task-lib/task');
import * as trl from "vsts-task-lib/toolrunner";

const TempFolder = '.certes';
const ProjContent = `
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <DotNetCliToolReference Include="Certes.Cli" Version="1.0.0" />
  </ItemGroup>
</Project>
`;

export class AuthorizationTask {

    private nugetSource: string;
    private acmeAccount: string;
    private indentifiers: string[];
    private directoryUri: string;
    private prviKey: string;

    constructor() {
        this.acmeAccount = tl.getInput("acmeAccount", true);
        this.indentifiers = tl.getDelimitedInput('identifiers', '\n', true);
        this.nugetSource = tl.getInput("nugetSource", false);

        this.directoryUri = tl.getEndpointAuthorizationParameter(this.acmeAccount, 'directoryUri', false);
        this.prviKey = tl.getEndpointAuthorizationParameter(this.acmeAccount, 'certificate', false);
    }

    public async execute() {
        tl.setResourcePath(path.join(__dirname, "task.json"));

        tl.debug('indentifiers for authz:');
        this.indentifiers.forEach(v => tl.debug(v));

        tl.mkdirP(`./${TempFolder}`);

        try {

            // minimal project file for restoring CLI as cli-tool
            tl.writeFile('certes.csproj', ProjContent);

            // call dotnet restore
            let dotnet = new trl.ToolRunner(tl.which("dotnet", true));
            dotnet.argIf(this.nugetSource, ['-s', this.nugetSource]);
            dotnet.arg('restore');
            await dotnet.exec();

            dotnet = new trl.ToolRunner(tl.which("dotnet", true));
            dotnet.arg('acme')
            await dotnet.exec();

            tl.setResult(tl.TaskResult.Succeeded, tl.loc("ScriptReturnCode", 0));
        }
        finally {
            tl.popd();
        }
    }
}

var tsk = new AuthorizationTask();
tsk.execute().catch((reason) => tl.setResult(tl.TaskResult.Failed, reason));
