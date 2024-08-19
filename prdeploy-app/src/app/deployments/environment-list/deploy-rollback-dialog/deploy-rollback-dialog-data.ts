import { Repository } from 'src/app/shared/models';

export interface DeployRollbackDialogData {
  repository: Repository;
  environment: string;
  pullNumber: number;
}
