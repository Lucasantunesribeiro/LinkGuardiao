import { FiGithub } from 'react-icons/fi';

const Footer = () => {
  return (
    <footer className="bg-gray-100 py-6">
      <div className="container-custom mx-auto">
        <div className="flex flex-col md:flex-row justify-between items-center">
          <div className="mb-4 md:mb-0">
            <p className="text-gray-700">© {new Date().getFullYear()} LinkGuardião. Todos os direitos reservados.</p>
          </div>
          <div className="flex items-center space-x-4">
            <a 
              href="https://github.com/yourusername/linkguardiao" 
              target="_blank" 
              rel="noopener noreferrer" 
              className="text-gray-700 hover:text-blue-600"
            >
              <FiGithub size={20} />
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;