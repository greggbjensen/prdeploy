import 'reflect-metadata';
import { afterAll } from 'vitest';
import cache from 'memory-cache';

// Clear memory cache after all tests to prevent worker process from hanging
afterAll(() => {
  cache.clear();
});

