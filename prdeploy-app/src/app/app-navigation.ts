export interface NavItem {
  text: string;
  path: string;
  icon: string;
  external?: boolean;
  expanded?: boolean;
  children?: NavItem[];
}

let currentNav: NavItem[] = [
  {
    text: 'Repositories',
    path: '/repositories',
    icon: 'folder_open'
  },
  {
    text: 'Help',
    path: 'https://prdeploy.readthedocs.io/en/latest/prdeploy-portal/',
    external: true,
    icon: 'help_outlined'
  }
];

export const navigation = (owner: string, repo: string) => {
  if (owner && repo && owner.length > 0 && repo.length > 0) {
    currentNav = [
      {
        text: 'Deployments',
        path: `${owner}/${repo}/deployments`,
        icon: 'cloud_upload'
      },
      {
        text: 'Environments',
        path: `${owner}/${repo}/environments`,
        icon: 'list_alt'
      },
      {
        text: 'Repositories',
        path: '/repositories',
        icon: 'folder_open'
      },
      {
        text: 'Settings',
        path: `${owner}/${repo}/settings`,
        icon: 'settings'
      },
      {
        text: 'Help',
        path: 'https://prdeploy.readthedocs.io/en/latest/prdeploy-portal/',
        external: true,
        icon: 'help_outlined'
      }
    ] as NavItem[];
  }

  return currentNav;
};
