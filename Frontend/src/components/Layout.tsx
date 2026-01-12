import React from 'react';
import { useLocation } from 'react-router-dom';
import Navbar from './Navbar';
import Footer from './Footer';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const location = useLocation();

  const isAuthPage = location.pathname === '/login' || location.pathname === '/register';

  if (isAuthPage) {
    return (
      <div className="app-shell">
        <div className="pointer-events-none absolute -top-40 left-[-20%] h-72 w-72 rounded-full bg-[radial-gradient(circle_at_center,rgba(242,166,90,0.45),rgba(242,166,90,0))] blur-2xl" aria-hidden />
        <div className="pointer-events-none absolute bottom-[-120px] right-[-10%] h-80 w-80 rounded-full bg-[radial-gradient(circle_at_center,rgba(19,122,108,0.35),rgba(19,122,108,0))] blur-2xl" aria-hidden />
        <main className="app-container py-16">
          {children}
        </main>
      </div>
    );
  }

  return (
    <div className="app-shell">
      <div className="pointer-events-none absolute -top-52 right-[-20%] h-96 w-96 rounded-full bg-[radial-gradient(circle_at_center,rgba(19,122,108,0.35),rgba(19,122,108,0))] blur-3xl animate-floaty" aria-hidden />
      <div className="pointer-events-none absolute top-24 left-[-15%] h-72 w-72 rounded-full bg-[radial-gradient(circle_at_center,rgba(242,166,90,0.35),rgba(242,166,90,0))] blur-3xl" aria-hidden />
      <Navbar />
      <main className="container-custom animate-rise">
        {children}
      </main>
      <Footer />
    </div>
  );
};

export default Layout;
