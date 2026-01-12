import React from 'react';

interface HeroProps {
  children: React.ReactNode;
}

export const Hero: React.FC<HeroProps> = ({ children }) => (
  <section className="relative overflow-hidden py-20">
    <div className="pointer-events-none absolute -top-24 right-8 h-56 w-56 rounded-full bg-[radial-gradient(circle_at_center,rgba(232,118,94,0.4),rgba(232,118,94,0))] blur-2xl" aria-hidden />
    <div className="pointer-events-none absolute bottom-[-90px] left-16 h-64 w-64 rounded-full bg-[radial-gradient(circle_at_center,rgba(19,122,108,0.35),rgba(19,122,108,0))] blur-2xl" aria-hidden />
    <div className="relative z-10">{children}</div>
  </section>
); 
