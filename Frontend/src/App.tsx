import { Suspense, lazy } from 'react';
import { Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import Loading from './components/Loading';

const HomePage = lazy(() => import('./pages/HomePage'));
const LoginPage = lazy(() => import('./pages/LoginPage'));
const RegisterPage = lazy(() => import('./pages/RegisterPage'));
const DashboardPage = lazy(() => import('./pages/DashboardPage'));
const EditLinkPage = lazy(() => import('./pages/EditLinkPage'));
const RedirectPage = lazy(() => import('./pages/RedirectPage'));
const LinkStatsPage = lazy(() => import('./pages/LinkStatsPage'));
const CreateLinkPage = lazy(() => import('./pages/CreateLinkPage'));
const NotFoundPage = lazy(() => import('./pages/NotFoundPage'));

function App() {
  return (
    <Layout>
      <Suspense
        fallback={
          <div className="flex items-center justify-center min-h-[60vh]">
            <Loading />
          </div>
        }
      >
        <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/r/:shortCode" element={<RedirectPage />} />
        <Route path="/:shortCode" element={<RedirectPage />} />

        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/edit-link/:id"
          element={
            <ProtectedRoute>
              <EditLinkPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/stats/:id"
          element={
            <ProtectedRoute>
              <LinkStatsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/create-link"
          element={
            <ProtectedRoute>
              <CreateLinkPage />
            </ProtectedRoute>
          }
        />

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
      </Suspense>
    </Layout>
  );
}

export default App;
