import { OAuthService } from 'angular-oauth2-oidc';
import { DeployStatusService, SseParser } from './deploy-status.service';

/** Verifies SSE parsing and that the service streams statuses with the bearer token. */
describe('SseParser', () => {
  it('parses an event and its data', () => {
    const messages = new SseParser().push('event: deploy-status\ndata: {"a":1}\n\n');
    expect(messages).toEqual([{ event: 'deploy-status', data: '{"a":1}' }]);
  });

  it('buffers partial chunks until the event is complete', () => {
    const parser = new SseParser();
    expect(parser.push('event: deploy-st')).toEqual([]);
    expect(parser.push('atus\ndata: 1\n')).toEqual([]);
    expect(parser.push('\n')).toEqual([{ event: 'deploy-status', data: '1' }]);
  });

  it('joins multi-line data, defaults the event name, and ignores comments and CRLF', () => {
    const messages = new SseParser().push(': keep-alive\r\ndata: a\r\ndata: b\r\n\r\n');
    expect(messages).toEqual([{ event: 'message', data: 'a\nb' }]);
  });

  it('parses several events from one chunk', () => {
    const messages = new SseParser().push('event: x\ndata: 1\n\nevent: y\ndata: 2\n\n');
    expect(messages.map(m => m.event)).toEqual(['x', 'y']);
  });
});

describe('DeployStatusService', () => {
  const status = { owner: 'o', repo: 'r', environment: 'dev', state: 'Successful', pullNumber: 1, runId: 2 };

  /** Builds a streaming fetch response from text chunks. */
  function streamResponse(chunks: string[], ok = true): Response {
    const encoder = new TextEncoder();
    const body = new ReadableStream<Uint8Array>({
      start(controller) {
        chunks.forEach(c => controller.enqueue(encoder.encode(c)));
        controller.close();
      }
    });
    return { ok, status: ok ? 200 : 403, body } as Response;
  }

  it('sends the bearer token and emits streamed statuses', async () => {
    const oauth = jasmine.createSpyObj<OAuthService>('OAuthService', ['getAccessToken']);
    oauth.getAccessToken.and.returnValue('token-1');
    const fetchSpy = spyOn(window, 'fetch').and.resolveTo(
      streamResponse([`event: deploy-status\ndata: ${JSON.stringify(status)}\n\n`])
    );

    const received = await new Promise<unknown>(resolve => {
      const subscription = new DeployStatusService(oauth).watch('o', 'r').subscribe(s => {
        subscription.unsubscribe();
        resolve(s);
      });
    });

    expect(received).toEqual(status);
    const [url, init] = fetchSpy.calls.mostRecent().args as [string, RequestInit];
    expect(url).toBe('/api/deploy-status/o/r/events');
    expect((init.headers as Record<string, string>)['Authorization']).toBe('Bearer token-1');
  });

  it('stops without reconnecting once unsubscribed', async () => {
    const oauth = jasmine.createSpyObj<OAuthService>('OAuthService', ['getAccessToken']);
    const fetchSpy = spyOn(window, 'fetch').and.resolveTo(streamResponse([], false));

    new DeployStatusService(oauth).watch('o', 'r').subscribe().unsubscribe();
    await new Promise(resolve => setTimeout(resolve, 20));

    expect(fetchSpy).toHaveBeenCalledTimes(1);
  });
});
