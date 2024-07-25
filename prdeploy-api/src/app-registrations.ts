import JiraApi from 'jira-client';
import dotenv from 'dotenv';
import { container } from 'tsyringe';
import { DEFAULT_SETTINGS_FILE, SSM_CLIENT } from './injection-tokens';
import { SSMClient } from '@aws-sdk/client-ssm';

// Load environment variables from .env file
console.log('Configuring environment.');
dotenv.config();

console.log('Adding app-wide registrations.');

container.register(JiraApi, {
  useFactory: () => {
    return new JiraApi({
      protocol: 'https',
      host: process.env.JIRA_HOST,
      username: process.env.JIRA_USERNAME,
      password: process.env.JIRA_PASSWORD,
      apiVersion: '2',
      strictSSL: true
    });
  }
});

container.register(SSM_CLIENT, {
  useFactory: () => {
    return new SSMClient({
      region: process.env.AWS_REGION,
      credentials: {
        accessKeyId: process.env.AWS_ACCESS_KEY_ID,
        secretAccessKey: process.env.AWS_SECRET_ACCESS_KEY
      }
    });
  }
});

container.register(DEFAULT_SETTINGS_FILE, { useValue: process.env.DEFAULT_SETTINGS_FILE });
