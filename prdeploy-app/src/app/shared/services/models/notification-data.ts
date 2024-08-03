import { ToastType } from 'devextreme/ui/toast';

export interface NotificationData {
  visible: boolean;
  message: string;
  type: ToastType;
}
