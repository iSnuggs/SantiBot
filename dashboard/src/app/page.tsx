import Link from "next/link";
import Image from "next/image";

export default function Home() {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-8 p-8">
      <div className="text-center">
        <Image
          src="/santi-logo.png"
          alt="SantiBot"
          width={128}
          height={128}
          className="mx-auto mb-4 rounded-2xl"
          priority
        />
        <h1 className="text-5xl font-bold mb-4">
          <span className="text-[var(--accent)]">Santi</span>Bot
        </h1>
        <p className="text-[var(--muted)] text-lg mb-8">
          The ultimate Discord bot dashboard
        </p>
      </div>

      <a
        href={`${apiUrl}/api/auth/login`}
        className="px-8 py-3 bg-[#5865F2] hover:bg-[#4752C4] text-white font-semibold rounded-lg transition-colors flex items-center gap-3"
      >
        <svg width="24" height="24" viewBox="0 0 71 55" fill="currentColor">
          <path d="M60.1045 4.8978C55.5792 2.8214 50.7265 1.2916 45.6527 0.41542C45.5603 0.39851 45.468 0.440769 45.4204 0.525289C44.7963 1.6353 44.105 3.0834 43.6209 4.2216C38.1637 3.4046 32.7345 3.4046 27.3892 4.2216C26.905 3.0581 26.1886 1.6353 25.5617 0.525289C25.5141 0.443589 25.4218 0.40133 25.3294 0.41542C20.2584 1.2888 15.4057 2.8186 10.8776 4.8978C10.8384 4.9147 10.8048 4.9429 10.7825 4.9795C1.57795 18.7309 -0.943561 32.1443 0.293408 45.3914C0.299005 45.4562 0.335386 45.5182 0.385761 45.5576C6.45866 50.0174 12.3413 52.7249 18.1147 54.5195C18.2071 54.5477 18.305 54.5139 18.3638 54.4378C19.7295 52.5728 20.9469 50.6063 21.9907 48.5383C22.0523 48.4172 21.9935 48.2735 21.8676 48.2256C19.9366 47.4931 18.0979 46.6 16.3292 45.5858C16.1893 45.5041 16.1781 45.304 16.3068 45.2082C16.679 44.9293 17.0513 44.6391 17.4067 44.3461C17.471 44.2926 17.5606 44.2813 17.6362 44.3151C29.2558 49.6202 41.8354 49.6202 53.3179 44.3151C53.3## 44.2785 53.4804 44.2898 53.5475 44.3433C53.9029 44.6363 54.2751 44.9293 54.6501 45.2082C54.7788 45.304 54.7676 45.5041 54.6277 45.5858C52.859 46.6197 51.0203 47.4931 49.0893 48.2228C48.9634 48.2707 48.9074 48.4172 48.969 48.5383C50.0349 50.6034 51.2523 52.5699 52.5929 54.435C52.6489 54.5139 52.7496 54.5477 52.842 54.5195C58.6426 52.7249 64.5253 50.0174 70.5982 45.5576C70.6513 45.5182 70.6849 45.459 70.6905 45.3942C72.1747 30.0791 68.2147 16.7757 60.1968 4.9823C60.1772 4.9429 60.1437 4.9147 60.1045 4.8978Z" />
        </svg>
        Login with Discord
      </a>

      <div className="mt-12 text-center text-[var(--muted)] text-sm">
        <p>Named after Santi, a beloved companion</p>
      </div>
    </main>
  );
}
