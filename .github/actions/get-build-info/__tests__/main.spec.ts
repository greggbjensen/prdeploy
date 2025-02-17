import * as process from 'process';
import * as cp from 'child_process';
import * as path from 'path';
import { expect, test } from '@jest/globals';

// shows how the runner will run a javascript action with env / stdout protocol
test.skip('test runs', () => {
  process.env['INPUT_VERSION'] = '1.0.0';
  const np = process.execPath;
  const ip = path.join(__dirname, '..', 'lib', 'main.js');
  const options: cp.ExecFileSyncOptions = {
    env: process.env
  };
  console.log(cp.execFileSync(np, [ip], options).toString());
});
