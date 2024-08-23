export interface NavItem {
  text: string;
  path: string;
  icon: string;
  expanded?: boolean;
  children?: NavItem[];
}

let currentNav = [
  {
    text: 'Repositories',
    path: '/repositories',
    icon: 'folder_open'
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
      }
    ] as NavItem[];
  }

  return currentNav;
};
