import * as mocha from 'mocha';
import * as path from 'path';
import * as assert from 'assert';
import * as ttm from 'vsts-task-lib/mock-test';

describe('authz task tests', function () {
    before(() => {
    });

    after(() => {
    });

    it('should succeed with simple inputs', (done: MochaDone) => {
        this.timeout(10000);

        let tp = path.join(__dirname, 'success.ts');
        let tr = new ttm.MockTestRunner(tp);

        tr.run();
        assert(tr.succeeded, 'should have succeeded');

        done();
    }); 
});

//https://gist.github.com/bryanmacfarlane/154f14dd8cb11a71ef04b0c836e5be6e
