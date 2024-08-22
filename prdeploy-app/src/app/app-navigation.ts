export interface NavItem {
  text: string;
  path: string;
  icon: string;
  expanded?: boolean;
  children?: NavItem[];
}

export const navigation = () =>
  [
    {
      text: 'Deployments',
      path: '/deployments',
      icon: 'cloud_upload'
    },
    {
      text: 'Environments',
      path: '/environments',
      icon: 'list_alt'
    },
    {
      text: 'Repositories',
      path: '/repositories',
      icon: 'folder_open'
    },
    {
      text: 'Settings',
      path: '/settings',
      icon: 'settings'
    }
  ] as NavItem[];
