module.exports = {
  clearMocks: true,
  moduleFileExtensions: ['js', 'ts'],
  testMatch: ['**/*.spec.ts'],
  transform: {
    '^.+\\.ts$': 'ts-jest',
    // Newer @octokit packages are ESM-only, so compile them to CommonJS for jest.
    '^.+\\.js$': ['ts-jest', { tsconfig: { allowJs: true, esModuleInterop: true } }]
  },
  transformIgnorePatterns: [
    '/node_modules/(?!(@octokit|universal-user-agent|before-after-hook|fast-content-type-parse|json-with-bigint|content-type)/)'
  ],
  verbose: true
};
