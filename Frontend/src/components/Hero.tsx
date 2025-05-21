import React from 'react';

interface HeroProps {
  children: React.ReactNode;
}

export const Hero: React.FC<HeroProps> = ({ children }) => (
  <section className="bg-gradient-to-r from-blue-600 to-blue-800 text-white py-20">
    <div className="container mx-auto text-center">{children}</div>
  </section>
); 