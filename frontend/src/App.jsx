import { useState } from 'react';

const letters = ['آ ا', 'ب', 'ا', 'د', 'س', 'ت', 'او', 'ر'];
const cardColors = [
  'bg-violet-500', 'bg-pink-500', 'bg-sky-600', 'bg-emerald-600',
  'bg-orange-500', 'bg-indigo-500', 'bg-rose-500', 'bg-teal-600',
];

export default function App() {
  const [showLetters, setShowLetters] = useState(false);

  const speakLetter = (letter) => {
    if (!('speechSynthesis' in window)) return;

    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(letter);
    utterance.lang = 'fa-IR';
    window.speechSynthesis.speak(utterance);
  };

  return (
    <main
      lang="fa"
      dir="rtl"
      className="relative isolate flex min-h-dvh items-center justify-center overflow-hidden bg-sky-100 p-6"
    >
      <div aria-hidden="true" className="pointer-events-none absolute -top-20 -right-16 size-72 rounded-full bg-yellow-200 sm:size-96" />
      <div aria-hidden="true" className="pointer-events-none absolute -bottom-28 -left-20 size-96 rounded-full bg-pink-200" />
      <div aria-hidden="true" className="pointer-events-none absolute top-1/4 left-[15%] size-8 rotate-12 rounded-xl bg-violet-300" />
      <div aria-hidden="true" className="pointer-events-none absolute right-[15%] bottom-1/4 size-6 rounded-full bg-orange-300" />

      {showLetters ? (
        <section aria-label="حروف دیکته" className="relative grid w-full max-w-4xl grid-cols-2 gap-5 py-6 sm:grid-cols-4 sm:gap-6">
          {letters.map((letter, index) => (
            <button
              key={letter}
              type="button"
              onClick={() => speakLetter(letter)}
              aria-label={`شنیدن حرف ${letter}`}
              className={`flex min-h-36 cursor-pointer items-center justify-center rounded-3xl border-4 border-white p-5 text-white shadow-lg transition-transform hover:-translate-y-1 hover:brightness-105 active:translate-y-0 active:scale-95 focus-visible:outline-4 focus-visible:outline-offset-4 focus-visible:outline-violet-700 sm:min-h-44 ${cardColors[index]}`}
            >
              <h2 className="text-5xl leading-normal font-bold sm:text-6xl">{letter}</h2>
            </button>
          ))}
        </section>
      ) : (
      <button
        type="button"
        onClick={() => setShowLetters(true)}
        aria-labelledby="dictation-title"
        className="relative flex min-h-72 w-full max-w-sm cursor-pointer flex-col items-center justify-center gap-6 rounded-[2.5rem] border-4 border-white bg-violet-500 px-8 py-12 text-white shadow-[0_16px_0_#c4b5fd,0_28px_50px_-12px_#8b5cf650] transition-colors hover:bg-violet-600 focus-visible:outline-4 focus-visible:outline-offset-8 focus-visible:outline-violet-700 sm:min-h-80"
      >
        <span aria-hidden="true" className="flex size-24 items-center justify-center rounded-3xl bg-white/20 text-6xl">✏️</span>
        <span id="dictation-title" className="text-6xl leading-normal font-bold sm:text-7xl">دیکته</span>
        <span aria-hidden="true" className="absolute top-7 left-8 text-3xl text-yellow-200">✦</span>
        <span aria-hidden="true" className="absolute right-8 bottom-8 text-2xl text-pink-200">✦</span>
      </button>
      )}
    </main>
  );
}
