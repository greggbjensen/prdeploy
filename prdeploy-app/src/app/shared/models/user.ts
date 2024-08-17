import { GitHubAccountType } from './github-account-type';

export interface User {
  id: number;
  avatarUrl: string;
  login: string;
  name: string;
  admin: boolean;
  type: GitHubAccountType;
}
