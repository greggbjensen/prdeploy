import dotenv from 'dotenv';
import fs from 'fs';

dotenv.config({ path: './.env' });

import { pathsToModuleNameMapper } from 'ts-jest';
import type { JestConfigWithTsJest } from 'ts-jest';
const jsConfig = JSON.parse(fs.readFileSync('./tsconfig.json').toString());
const compilerOptions = jsConfig.compilerOptions;

const jestConfig: JestConfigWithTsJest = {
  preset: 'ts-jest',
  clearMocks: true,
  moduleFileExtensions: ['ts', 'js'],
  testMatch: ['**/*.spec.ts'],
  testTimeout: 15000,
  transform: {
    '^.+\\.ts$': 'ts-jest',
    'ts-jest': [
      'ts-jest',
      {
        tsconfig: 'tsconfig.spec.json'
      }
    ]
  },
  verbose: true,
  roots: ['<rootDir>/src', '<rootDir>/test'],
  modulePaths: [compilerOptions.baseUrl], // <-- This will be set to 'baseUrl' value
  moduleDirectories: ['node_modules', '@src'],
  moduleNameMapper: pathsToModuleNameMapper(compilerOptions.paths, { prefix: '<rootDir>/' }),
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  globals: {}
};

export default jestConfig;
