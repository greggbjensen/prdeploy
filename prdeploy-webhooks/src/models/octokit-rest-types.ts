import { RestEndpointMethodTypes } from '@octokit/rest';

// Responses.
export type PullRequest = RestEndpointMethodTypes['pulls']['get']['response']['data'];
export type User = RestEndpointMethodTypes['pulls']['get']['response']['data']['user'];
export type Environment = RestEndpointMethodTypes['repos']['getAllEnvironments']['response']['data']['environments'][0];
export type CheckRun = RestEndpointMethodTypes['checks']['listForRef']['response']['data']['check_runs'][0];

// Params.
export type CreateForIssueCommentParams = RestEndpointMethodTypes['reactions']['createForIssueComment']['parameters'];
export type CreateWorkflowDispatchParams = RestEndpointMethodTypes['actions']['createWorkflowDispatch']['parameters'];
export type CreateCommentParams = RestEndpointMethodTypes['issues']['createComment']['parameters'];
export type GetContentParams = RestEndpointMethodTypes['repos']['getContent']['parameters'];
