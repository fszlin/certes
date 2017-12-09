import * as path from "path";
import * as tl from 'vsts-task-lib/task';
import * as trl from "vsts-task-lib/toolrunner";
import * as st from "string-template";

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
        this.acmeAccount = tl.getInput("acmeAccount", true);
        this.indentifiers = tl.getDelimitedInput('identifiers', '\n', true);
        this.nugetSource = tl.getInput("nugetSource", false);
        this.frameworkVersion = tl.getInput("frameworkVersion", false);
        this.cliVersion = tl.getInput("cliVersion", true);

        this.directoryUri = tl.getEndpointAuthorizationParameter(this.acmeAccount, 'directoryUri', false);
        this.prviKey = tl.getEndpointAuthorizationParameter(this.acmeAccount, 'certificate', false);
    }

    public async execute() {
        tl.setResourcePath(path.join(__dirname, "task.json"));

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

            const dotnetPath = tl.which("dotnet", true);

            // call dotnet restore
            let dotnet = new trl.ToolRunner(dotnetPath);
            dotnet.arg('restore');
            dotnet.argIf(this.nugetSource, ['-s', this.nugetSource]);
            await dotnet.exec();

            dotnet = new trl.ToolRunner(dotnetPath);
            dotnet.arg(['acme', 'authz', '--server', this.directoryUri]);
            this.indentifiers.forEach(v => dotnet.arg(['--v', v]));
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
