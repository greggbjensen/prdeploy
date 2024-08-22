import { NotificationType } from './notification-type';

export interface NotificationData {
  visible: boolean;
  message: string;
  type: NotificationType;
}
