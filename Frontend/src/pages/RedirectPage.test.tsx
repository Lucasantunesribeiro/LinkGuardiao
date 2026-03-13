import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import RedirectPage from './RedirectPage';
import { redirectToUrl } from '../lib/navigation';

vi.mock('../lib/navigation', () => ({
  redirectToUrl: vi.fn()
}));

type MockFetchResponse = {
  ok: boolean;
  status: number;
  json: () => Promise<unknown>;
};

const renderRedirectPage = (shortCode = 'abc123') => {
  render(
    <MemoryRouter initialEntries={[`/r/${shortCode}`]}>
      <Routes>
        <Route path="/r/:shortCode" element={<RedirectPage />} />
      </Routes>
    </MemoryRouter>
  );
};

describe('RedirectPage', () => {
  const fetchMock = vi.fn<(input: RequestInfo | URL, init?: RequestInit) => Promise<MockFetchResponse>>();

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    fetchMock.mockReset();
    vi.mocked(redirectToUrl).mockReset();
    vi.unstubAllGlobals();
  });

  it('redirects immediately when the public lookup is not password protected', async () => {
    fetchMock.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({
        shortCode: 'abc123',
        isPasswordProtected: false
      })
    });

    renderRedirectPage();

    await waitFor(() => {
      expect(redirectToUrl).toHaveBeenCalledWith('http://localhost:5000/r/abc123');
    });
  });

  it('requests an access grant and redirects through the official redirect endpoint', async () => {
    const user = userEvent.setup();

    fetchMock
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          shortCode: 'secure01',
          isPasswordProtected: true
        })
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          accessGrant: 'grant-token',
          expiresInSeconds: 60
        })
      });

    renderRedirectPage('secure01');

    const passwordInput = await screen.findByLabelText('Senha');
    await user.type(passwordInput, 'super-secret');
    await user.click(screen.getByRole('button', { name: 'Acessar' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenLastCalledWith('http://localhost:5000/api/links/access-grant/secure01', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ password: 'super-secret' })
      });
      expect(redirectToUrl).toHaveBeenCalledWith('http://localhost:5000/r/secure01?grant=grant-token');
    });
  });

  it('shows an explicit error when the password is invalid', async () => {
    const user = userEvent.setup();

    fetchMock
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({
          shortCode: 'secure02',
          isPasswordProtected: true
        })
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 401,
        json: async () => ({})
      });

    renderRedirectPage('secure02');

    await user.type(await screen.findByLabelText('Senha'), 'wrong-password');
    await user.click(screen.getByRole('button', { name: 'Acessar' }));

    expect(await screen.findByText('Senha incorreta. Tente novamente.')).toBeInTheDocument();
    expect(redirectToUrl).not.toHaveBeenCalled();
  });
});
