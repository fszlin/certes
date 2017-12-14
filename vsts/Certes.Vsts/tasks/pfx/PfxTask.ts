import * as path from 'path';
import * as tl from 'vsts-task-lib/task';

const TempFolder = '.certes';

export class PfxTask {

    constructor() {
    }

    public async execute() {
    }
}

var tsk = new PfxTask();
tsk.execute().catch((reason) => tl.setResult(tl.TaskResult.Failed, reason));
