import type { CodegenConfig } from '@graphql-codegen/cli';

const config: CodegenConfig = {
  overwrite: true,
  schema: 'http://localhost:5197/graphql/',
  documents: 'src/app/shared/graphql/deploy-api/**/*.graphql',
  generates: {
    'src/app/shared/graphql/generated/deploy-api-graphql.ts': {
      plugins: ['typescript', 'typescript-operations', 'typescript-apollo-angular'],
      config: {
        namedClient: 'deploy',
        serviceProvidedInRoot: true,
        addExplicitOverride: true
      }
    }
  }
};
export default config;
