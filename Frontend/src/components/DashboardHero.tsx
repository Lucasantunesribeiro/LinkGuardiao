import React from 'react';

interface DashboardHeroProps {
  children: React.ReactNode;
}

export const DashboardHero: React.FC<DashboardHeroProps> = ({ children }) => (
  <section className="rounded-3xl border border-white/70 bg-white/70 px-6 py-10 shadow-soft-xl backdrop-blur">
    <div className="app-container">{children}</div>
  </section>
); 
