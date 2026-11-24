# What we learned building an offline schematic reader

From the prototype rounds of 2026-11-23/24 (Python, then ported into the app as assisted
reading). Numbers are measured on the test card 2A50 (NORD-10 drawing book page 64) and on 10
work pages that were keyed by hand. "Inferred" marks an explanation that was not proved.

## The result in one line

A small offline network finds chips, gates, circles and position codes fairly well, but gets
only 4-6 % of the real wire connections, because it cannot read the tiny pin numbers at wire
ends. So the app does **assisted reading**: it marks what it finds, and a person or an LLM
agent connects wire ends to pins. Every card read that way becomes training data.

## Why an LLM reads the drawing better than the small network

These points are a general understanding of the models, not measured here (inferred).

1. **Training size.** The LLM learned from a vast amount of images and text, including
   handwriting, circuit drawings and datasheets. The small networks learned from a few hundred
   thousand cut-out ND characters and pages we generated ourselves.
2. **It knows what should be there.** "13D" beside pins 15, 14, 13, 12 is a 7442, so a smudged
   digit is read as the only answer that fits. The small network judges each blob alone, with
   no knowledge of chips, pinouts or ND naming.
3. **It reasons over the whole page.** It follows a wire, notices a signal that goes nowhere
   and calls it wrong. The pipeline makes thousands of local decisions, and one early mistake
   (a misread pin number) flows straight through.
4. **It checks itself** - when told to. The good readings zoomed to 1200 dpi and checked
   against datasheets, the parts list and the driver software (ICW₀, 11D pin 11, BD5 = BD8).

**Its limits:** the first LLM reading had ICW₀ wrong and 11D pin 11 stayed wrong until a
second reading. Answer keys made quickly from overview pictures were poor and had to be fixed.
What made it reliable was zooming, rule checks and a second reader - not the model alone. It is
also slow, costly per page and needs a connection.

## What the measurements taught

- **Measure connections, not pins.** A score that counted pins "in the right net" looked like
  36-64 % - but most of those pins sat alone on a one-pin net and joined nothing. Counting
  pairs of pins that should be joined gave the honest 4-6 % (found) and 17-29 % (right).
- **Bad answer keys poison training and scoring.** Keys made from overview pictures had to be
  checked against hard rules (pin exists in the pinout, one pin one net, gate pins form one
  gate) and re-read at 1200 dpi.
- **Measure where the loss is before fixing.** The guess "wire tracing is the problem" was
  wrong; the losses were position codes (biggest) and pin numbers.
- **Generated training pages help, but must look like the real ones.** The hand buffer and
  the hand inverter were drawn alike in the generator, so the network confused them on real
  pages. Real lettering turned out thinner, not heavier, than guessed.
- **Every fix that assigns more also assigns more wrongly.** Right and wrong pins rose
  together; a wrong pin costs more than an empty one, because someone has to find it.
- **ND-100 drawings are not all modern.** The ND-100 binder and the I/O-system book are
  hand-drawn in the old ND style; only ND-120 and the SCSI card are CAD. The split is by
  drawing style, not by machine. One shared model beat one-model-per-style.
- **ND-100 board grid** is columns 1-29, rows A-H and J (no I); NORD-10 is 1-19, A-E. Reading
  codes on the wrong grid turned G into C.
- **Page turning works** from the character network's confidence at each quarter turn
  (245 of 250 pages right); pale scans need a contrast stretch first.

## Where it stands

- Offline: chips and gates mostly found; about half the position codes read; connections
  4-6 %.
- Plan: assisted reading in the app (finds marked, people/agents connect pins), each accepted
  box and pin saved as training data, and the automatic part grown from real read cards.
