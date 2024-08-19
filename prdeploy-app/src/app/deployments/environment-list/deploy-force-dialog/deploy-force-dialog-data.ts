import { Repository } from 'src/app/shared/models';

export interface DeployForceDialogData {
  repository: Repository;
  environment: string;
}
