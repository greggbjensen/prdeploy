import { afterEach, beforeEach, describe, expect, it, jest } from '@jest/globals';
import * as core from '@actions/core';
import { ServiceNameService } from '../src/services';

jest.mock('@actions/core');

/** Replaces the global fetch with a mock returning the given response. */
function mockFetch(response: { ok: boolean; status?: number; body?: unknown } | Error) {
  const fetchMock = jest.fn(async (_url: string, _init?: unknown) => {
    if (response instanceof Error) throw response;
    return { ok: response.ok, status: response.status ?? 200, json: async () => response.body };
  });
  (global as any).fetch = fetchMock;
  return fetchMock;
}

describe('ServiceNameService.list', () => {
  const originalFetch = (global as any).fetch;
  beforeEach(() => jest.mocked(core.warning).mockClear());
  afterEach(() => {
    (global as any).fetch = originalFetch;
  });

  it('does not call the API when no URL is configured', async () => {
    const fetchMock = mockFetch({ ok: true, body: [] });

    expect(await new ServiceNameService('  ', 't').list('o', 'r')).toEqual({});
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('keys the mapped names by lowercase workflow file and sends the token', async () => {
    const fetchMock = mockFetch({ ok: true, body: [{ workflow: 'Api-CI.yml', name: 'api' }] });

    const names = await new ServiceNameService('https://pr.example/', 'tok').list('my org', 'r');

    expect(names).toEqual({ 'api-ci.yml': 'api' });
    expect(fetchMock).toHaveBeenCalledWith('https://pr.example/api/build-names/my%20org/r', {
      headers: { Authorization: 'Bearer tok' }
    });
  });

  it.each([
    ['a non-success status', { ok: false, status: 403 }, 'returned 403'],
    ['a network error', new Error('offline'), 'offline']
  ])('warns and returns no names on %s', async (_label, response, message) => {
    mockFetch(response as any);

    expect(await new ServiceNameService('https://pr.example', 't').list('o', 'r')).toEqual({});
    expect(core.warning).toHaveBeenCalledWith(expect.stringContaining(message));
  });
});
