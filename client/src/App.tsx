import { Navigate, NavLink, Route, Routes } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AuthProvider, useAuth } from './auth';
import LoginPage from './pages/LoginPage';
import DocumentsPage from './pages/DocumentsPage';
import UploadPage from './pages/UploadPage';
import TypesPage from './pages/TypesPage';
import ChatPage from './pages/ChatPage';

function Protected({ children }: { children: JSX.Element }) {
  const { token } = useAuth();
  return token ? children : <Navigate to="/login" replace />;
}

function Layout({ children }: { children: JSX.Element }) {
  const { token, email, logout } = useAuth();
  const { t, i18n } = useTranslation();

  if (!token) return children;

  return (
    <>
      <nav className="nav">
        <strong>{t('app.title')}</strong>
        <NavLink to="/documents" className={({ isActive }) => (isActive ? 'active' : '')}>{t('nav.documents')}</NavLink>
        <NavLink to="/upload" className={({ isActive }) => (isActive ? 'active' : '')}>{t('nav.upload')}</NavLink>
        <NavLink to="/types" className={({ isActive }) => (isActive ? 'active' : '')}>{t('nav.types')}</NavLink>
        <NavLink to="/chat" className={({ isActive }) => (isActive ? 'active' : '')}>{t('nav.chat')}</NavLink>
        <span className="spacer" />
        <select
          value={i18n.language.startsWith('vi') ? 'vi' : 'en'}
          onChange={(e) => i18n.changeLanguage(e.target.value)}
          style={{ width: 90, color: '#1f2937' }}
        >
          <option value="en">EN</option>
          <option value="vi">VI</option>
        </select>
        <span style={{ marginLeft: 12 }}>{email}</span>
        <button className="btn secondary" onClick={logout} style={{ marginLeft: 8 }}>
          {t('nav.logout')}
        </button>
      </nav>
      <div className="container">{children}</div>
    </>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <Layout>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/documents" element={<Protected><DocumentsPage /></Protected>} />
          <Route path="/upload" element={<Protected><UploadPage /></Protected>} />
          <Route path="/types" element={<Protected><TypesPage /></Protected>} />
          <Route path="/chat" element={<Protected><ChatPage /></Protected>} />
          <Route path="*" element={<Navigate to="/documents" replace />} />
        </Routes>
      </Layout>
    </AuthProvider>
  );
}
