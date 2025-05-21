import React from 'react';

interface DashboardHeroProps {
  children: React.ReactNode;
}

export const DashboardHero: React.FC<DashboardHeroProps> = ({ children }) => (
  <section className="bg-blue-100 py-16 text-center">
    <div className="container mx-auto">{children}</div>
  </section>
); 