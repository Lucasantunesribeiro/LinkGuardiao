import React from 'react';

interface MainLayoutProps {
  children: React.ReactNode;
}

export const MainLayout: React.FC<MainLayoutProps> = ({ children }) => (
  <div className="min-h-screen flex flex-col">
    {children}
  </div>
); 
