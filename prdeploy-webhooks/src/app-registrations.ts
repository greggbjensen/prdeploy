import dotenv from 'dotenv';
import { container } from 'tsyringe';
import { SSM_CLIENT } from './injection-tokens';
import { SSMClient } from '@aws-sdk/client-ssm';

// Load environment variables from .env file
console.log('Configuring environment.');
dotenv.config();

console.log('Adding app-wide registrations.');
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
