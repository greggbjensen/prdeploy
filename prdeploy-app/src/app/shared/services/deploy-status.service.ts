import { Injectable } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { Observable } from 'rxjs';
import { DeployWorkflowStatus } from '../models';
import { AuthService } from './auth/auth.service';

/** A single Server-Sent Event. */
export interface SseMessage {
  event: string;
  data: string;
}

/** SSE event name carrying a {@link DeployWorkflowStatus}. */
export const DEPLOY_STATUS_EVENT = 'deploy-status';

/** Delay before reconnecting a dropped stream. */
export const RECONNECT_DELAY_MS = 3000;

/**
 * Incrementally parses a Server-Sent Events text stream.
 * Feed it chunks with {@link SseParser.push}; it returns the messages completed by each chunk.
 */
export class SseParser {
  private _buffer = '';
  private _event = '';
  private _data: string[] = [];

  /** Adds a chunk of text and returns every message it completes. */
  push(chunk: string): SseMessage[] {
    this._buffer += chunk;
    const messages: SseMessage[] = [];
    const lines = this._buffer.split(/\r\n|\n|\r/);
    // The last element is an unfinished line (or empty); keep it for the next chunk.
    this._buffer = lines.pop() ?? '';

    for (const line of lines) {
      if (line === '') {
        if (this._data.length > 0) {
          messages.push({ event: this._event || 'message', data: this._data.join('\n') });
        }
        this._event = '';
        this._data = [];
      } else if (line.startsWith(':')) {
        continue; // Comment or keep-alive.
      } else {
        const separator = line.indexOf(':');
        const field = separator < 0 ? line : line.substring(0, separator);
        let value = separator < 0 ? '' : line.substring(separator + 1);
        if (value.startsWith(' ')) {
          value = value.substring(1);
        }

        if (field === 'event') {
          this._event = value;
        } else if (field === 'data') {
          this._data.push(value);
        }
      }
    }

    return messages;
  }
}

/**
 * Streams deployment workflow status for a repository from the API using Server-Sent Events.
 * Uses fetch rather than EventSource so the bearer token can be sent in the Authorization header.
 * Because fetch bypasses the HTTP interceptors, a 401 or 403 is handled here the same way:
 * the stream stops reconnecting and the user is logged out.
 */
@Injectable({
  providedIn: 'root'
})
export class DeployStatusService {
  constructor(
    private _oauthService: OAuthService,
    private _authService: AuthService
  ) {}

  /**
   * Watches the deploy status of a repository. The stream reconnects after failures and closes when unsubscribed.
   * @param owner Repository owner or organization.
   * @param repo Repository name.
   */
  watch(owner: string, repo: string): Observable<DeployWorkflowStatus> {
    return new Observable<DeployWorkflowStatus>(subscriber => {
      const abort = new AbortController();
      let timer: ReturnType<typeof setTimeout> | undefined;

      const connect = async (): Promise<void> => {
        try {
          const response = await fetch(
            `/api/deploy-status/${encodeURIComponent(owner)}/${encodeURIComponent(repo)}/events`,
            {
              headers: {
                Accept: 'text/event-stream',
                Authorization: `Bearer ${this._oauthService.getAccessToken()}`
              },
              signal: abort.signal
            }
          );

          if (response.status === 401 || response.status === 403) {
            // Retrying with the same rejected token would fail forever; send the user to log in instead.
            subscriber.complete();
            this._authService.logout(response.status === 403);
            return;
          }

          if (!response.ok || !response.body) {
            throw new Error(`Deploy status stream failed with ${response.status}.`);
          }

          const reader = response.body.getReader();
          const decoder = new TextDecoder();
          const parser = new SseParser();
          for (;;) {
            const { done, value } = await reader.read();
            if (done) {
              break;
            }

            for (const message of parser.push(decoder.decode(value, { stream: true }))) {
              if (message.event === DEPLOY_STATUS_EVENT) {
                subscriber.next(JSON.parse(message.data) as DeployWorkflowStatus);
              }
            }
          }
        } catch {
          // Fall through to reconnect unless we were closed on purpose.
        }

        if (!abort.signal.aborted) {
          timer = setTimeout(connect, RECONNECT_DELAY_MS);
        }
      };

      void connect();

      return () => {
        abort.abort();
        if (timer) {
          clearTimeout(timer);
        }
      };
    });
  }
}
