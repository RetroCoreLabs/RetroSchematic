# Pin rename of 2026-11-23

Every change the script made when inverted pin names (`1Q'`) became a base name with
`"inverted": true`, written `/NAME` in formulas and pin fields (`catalog/chips/FORMAT.md`, Pins).
One line per change: chip, place in the file, old text, new text. `ChipPinNameTests` checks that
every formula gives the same truth table as before.

```text
74107 pin 2: name 1Q'  ->  1Q, inverted
74107 pin 6: name 2Q'  ->  2Q, inverted
74107 functions[1].formula: 1Q' = NOT FF1  ->  /1Q = NOT FF1
74107 functions[3].formula: 2Q' = NOT FF2  ->  /2Q = NOT FF2
74107 outputs[1].pin: 1Q'  ->  /1Q
74107 outputs[1].formula: 1Q' = NOT FF1  ->  /1Q = NOT FF1
74107 outputs[3].pin: 2Q'  ->  /2Q
74107 outputs[3].formula: 2Q' = NOT FF2  ->  /2Q = NOT FF2
74120 pin 7: name 1Y'  ->  1Y, inverted
74120 pin 9: name 2Y'  ->  2Y, inverted
74120 outputs[1].pin: 1Y'  ->  /1Y
74120 outputs[1].formula: 1Y' = NOT(1C AND EN1)  ->  /1Y = NOT(1C AND EN1)
74120 outputs[3].pin: 2Y'  ->  /2Y
74120 outputs[3].formula: 2Y' = NOT(2C AND EN2)  ->  /2Y = NOT(2C AND EN2)
74121 pin 1: name Q'  ->  Q, inverted
74121 functions[1].formula: Q' = NOT PULSE  ->  /Q = NOT PULSE
74121 timing.to: Q'  ->  /Q
74121 timing.to: Q'  ->  /Q
74121 outputs[1].pin: Q'  ->  /Q
74121 outputs[1].formula: Q' = NOT P  ->  /Q = NOT P
74123 pin 4: name 1Q'  ->  1Q, inverted
74123 pin 12: name 2Q'  ->  2Q, inverted
74123 functions[1].formula: 1Q' = NOT PULSE1  ->  /1Q = NOT PULSE1
74123 functions[3].formula: 2Q' = NOT PULSE2  ->  /2Q = NOT PULSE2
74123 outputs[1].pin: 1Q'  ->  /1Q
74123 outputs[1].formula: 1Q' = NOT P1  ->  /1Q = NOT P1
74123 outputs[3].pin: 2Q'  ->  /2Q
74123 outputs[3].formula: 2Q' = NOT P2  ->  /2Q = NOT P2
74165 pin 7: name QH'  ->  QH, inverted
74165 functions[1].formula: QH' = NOT QH  ->  /QH = NOT QH
74165 timing.to: QH'  ->  /QH
74165 timing.to: QH'  ->  /QH
74165 outputs[1].pin: QH'  ->  /QH
74165 outputs[1].formula: QH' = NOT FH  ->  /QH = NOT FH
74175 pin 3: name 1Q'  ->  1Q, inverted
74175 pin 6: name 2Q'  ->  2Q, inverted
74175 pin 11: name 3Q'  ->  3Q, inverted
74175 pin 14: name 4Q'  ->  4Q, inverted
74175 functions[1].formula: 1Q' = NOT FF1  ->  /1Q = NOT FF1
74175 functions[3].formula: 2Q' = NOT FF2  ->  /2Q = NOT FF2
74175 functions[5].formula: 3Q' = NOT FF3  ->  /3Q = NOT FF3
74175 functions[7].formula: 4Q' = NOT FF4  ->  /4Q = NOT FF4
74175 outputs[1].pin: 1Q'  ->  /1Q
74175 outputs[1].formula: 1Q' = NOT FF1  ->  /1Q = NOT FF1
74175 outputs[3].pin: 2Q'  ->  /2Q
74175 outputs[3].formula: 2Q' = NOT FF2  ->  /2Q = NOT FF2
74175 outputs[5].pin: 3Q'  ->  /3Q
74175 outputs[5].formula: 3Q' = NOT FF3  ->  /3Q = NOT FF3
74175 outputs[7].pin: 4Q'  ->  /4Q
74175 outputs[7].formula: 4Q' = NOT FF4  ->  /4Q = NOT FF4
74181 pin 2: name A0'  ->  A0, inverted
74181 pin 23: name A1'  ->  A1, inverted
74181 pin 21: name A2'  ->  A2, inverted
74181 pin 19: name A3'  ->  A3, inverted
74181 pin 1: name B0'  ->  B0, inverted
74181 pin 22: name B1'  ->  B1, inverted
74181 pin 20: name B2'  ->  B2, inverted
74181 pin 18: name B3'  ->  B3, inverted
74181 pin 9: name F0'  ->  F0, inverted
74181 pin 10: name F1'  ->  F1, inverted
74181 pin 11: name F2'  ->  F2, inverted
74181 pin 13: name F3'  ->  F3, inverted
74181 pin 15: name P'  ->  P, inverted
74181 pin 17: name G'  ->  G, inverted
74181 functions[2].formula: P' = carry propagate of the 4-bit group (for the '182)  ->  /P = carry propagate of the 4-bit group (for the '182)
74181 functions[3].formula: G' = carry generate of the 4-bit group (for the '182)  ->  /G = carry generate of the 4-bit group (for the '182)
74181 timing.to: G'  ->  /G
74181 timing.to: G'  ->  /G
74181 timing.to: G'  ->  /G
74181 timing.to: G'  ->  /G
74181 timing.to: P'  ->  /P
74181 timing.to: P'  ->  /P
74181 timing.to: P'  ->  /P
74181 timing.to: P'  ->  /P
74181 outputs[0].pin: F0'  ->  /F0
74181 outputs[0].formula: F0' = U0 XOR L0 XOR K0  ->  /F0 = U0 XOR L0 XOR K0
74181 outputs[1].pin: F1'  ->  /F1
74181 outputs[1].formula: F1' = U1 XOR L1 XOR K1  ->  /F1 = U1 XOR L1 XOR K1
74181 outputs[2].pin: F2'  ->  /F2
74181 outputs[2].formula: F2' = U2 XOR L2 XOR K2  ->  /F2 = U2 XOR L2 XOR K2
74181 outputs[3].pin: F3'  ->  /F3
74181 outputs[3].formula: F3' = U3 XOR L3 XOR K3  ->  /F3 = U3 XOR L3 XOR K3
74181 outputs[4].pin: P'  ->  /P
74181 outputs[4].formula: P' = NOT(U0 AND U1 AND U2 AND U3)  ->  /P = NOT(U0 AND U1 AND U2 AND U3)
74181 outputs[5].pin: G'  ->  /G
74181 outputs[5].formula: G' = NOT(L3 OR (U3 AND L2) OR (U3 AND U2 AND L1) OR (U3 AND U2 AND U1 AND L0))  ->  /G = NOT(L3 OR (U3 AND L2) OR (U3 AND U2 AND L1) OR (U3 AND U2 AND U1 AND L0))
74181 outputs[6].formula: Cn+4 = NOT G' OR (U0 AND U1 AND U2 AND U3 AND Cn)  ->  Cn+4 = NOT /G OR (U0 AND U1 AND U2 AND U3 AND Cn)
74181 outputs[7].formula: A=B = F0' AND F1' AND F2' AND F3'  ->  A=B = /F0 AND /F1 AND /F2 AND /F3
74181 internal[1]: U0 = NOT((A0' AND B0' AND S3) OR (A0' AND NOT B0' AND S2))  ->  U0 = NOT((/A0 AND /B0 AND S3) OR (/A0 AND NOT /B0 AND S2))
74181 internal[2]: L0 = NOT((NOT B0' AND S1) OR (B0' AND S0) OR A0')  ->  L0 = NOT((NOT /B0 AND S1) OR (/B0 AND S0) OR /A0)
74181 internal[3]: U1 = NOT((A1' AND B1' AND S3) OR (A1' AND NOT B1' AND S2))  ->  U1 = NOT((/A1 AND /B1 AND S3) OR (/A1 AND NOT /B1 AND S2))
74181 internal[4]: L1 = NOT((NOT B1' AND S1) OR (B1' AND S0) OR A1')  ->  L1 = NOT((NOT /B1 AND S1) OR (/B1 AND S0) OR /A1)
74181 internal[5]: U2 = NOT((A2' AND B2' AND S3) OR (A2' AND NOT B2' AND S2))  ->  U2 = NOT((/A2 AND /B2 AND S3) OR (/A2 AND NOT /B2 AND S2))
74181 internal[6]: L2 = NOT((NOT B2' AND S1) OR (B2' AND S0) OR A2')  ->  L2 = NOT((NOT /B2 AND S1) OR (/B2 AND S0) OR /A2)
74181 internal[7]: U3 = NOT((A3' AND B3' AND S3) OR (A3' AND NOT B3' AND S2))  ->  U3 = NOT((/A3 AND /B3 AND S3) OR (/A3 AND NOT /B3 AND S2))
74181 internal[8]: L3 = NOT((NOT B3' AND S1) OR (B3' AND S0) OR A3')  ->  L3 = NOT((NOT /B3 AND S1) OR (/B3 AND S0) OR /A3)
74182 pin 3: name G0'  ->  G0, inverted
74182 pin 1: name G1'  ->  G1, inverted
74182 pin 14: name G2'  ->  G2, inverted
74182 pin 5: name G3'  ->  G3, inverted
74182 pin 4: name P0'  ->  P0, inverted
74182 pin 2: name P1'  ->  P1, inverted
74182 pin 15: name P2'  ->  P2, inverted
74182 pin 6: name P3'  ->  P3, inverted
74182 pin 10: name G'  ->  G, inverted
74182 pin 7: name P'  ->  P, inverted
74182 functions[3].formula: G' = NOT (G3 OR (P3 AND G2) OR (P3 AND P2 AND G1) OR (P3 AND P2 AND P1 AND G0))  ->  /G = NOT (G3 OR (P3 AND G2) OR (P3 AND P2 AND G1) OR (P3 AND P2 AND P1 AND G0))
74182 functions[4].formula: P' = NOT (P3 AND P2 AND P1 AND P0)  ->  /P = NOT (P3 AND P2 AND P1 AND P0)
74182 outputs[0].formula: Cn+x = (NOT G0') OR ((NOT P0') AND Cn)  ->  Cn+x = (NOT /G0) OR ((NOT /P0) AND Cn)
74182 outputs[1].formula: Cn+y = (NOT G1') OR ((NOT P1') AND (NOT G0')) OR ((NOT P1') AND (NOT P0') AND Cn)  ->  Cn+y = (NOT /G1) OR ((NOT /P1) AND (NOT /G0)) OR ((NOT /P1) AND (NOT /P0) AND Cn)
74182 outputs[2].formula: Cn+z = (NOT G2') OR ((NOT P2') AND (NOT G1')) OR ((NOT P2') AND (NOT P1') AND (NOT G0')) OR ((NOT P2') AND (NOT P1') AND (NOT P0') AND Cn)  ->  Cn+z = (NOT /G2) OR ((NOT /P2) AND (NOT /G1)) OR ((NOT /P2) AND (NOT /P1) AND (NOT /G0)) OR ((NOT /P2) AND (NOT /P1) AND (NOT /P0) AND Cn)
74182 outputs[3].pin: G'  ->  /G
74182 outputs[3].formula: G' = NOT ((NOT G3') OR ((NOT P3') AND (NOT G2')) OR ((NOT P3') AND (NOT P2') AND (NOT G1')) OR ((NOT P3') AND (NOT P2') AND (NOT P1') AND (NOT G0')))  ->  /G = NOT ((NOT /G3) OR ((NOT /P3) AND (NOT /G2)) OR ((NOT /P3) AND (NOT /P2) AND (NOT /G1)) OR ((NOT /P3) AND (NOT /P2) AND (NOT /P1) AND (NOT /G0)))
74182 outputs[4].pin: P'  ->  /P
74182 outputs[4].formula: P' = NOT ((NOT P3') AND (NOT P2') AND (NOT P1') AND (NOT P0'))  ->  /P = NOT ((NOT /P3) AND (NOT /P2) AND (NOT /P1) AND (NOT /P0))
74191 pin 11: name LOAD'  ->  LOAD, inverted
74191 pin 4: name G'  ->  G, inverted
74191 pin 5: name D/U'  ->  D/U, inverted
74191 pin 13: name RIPPLE CLOCK'  ->  RIPPLE CLOCK, inverted
74191 functions[1].formula: MAX/MIN = (D/U' = L AND Q = 15) OR (D/U' = H AND Q = 0)  ->  MAX/MIN = (/D/U = L AND Q = 15) OR (/D/U = H AND Q = 0)
74191 functions[2].formula: RIPPLE CLOCK' = NOT (MAX/MIN AND G' = L AND CLOCK = L)  ->  /RIPPLE CLOCK = NOT (MAX/MIN AND /G = L AND CLOCK = L)
74191 timing.from: LOAD'  ->  /LOAD
74191 timing.from: LOAD'  ->  /LOAD
74191 timing.to: RIPPLE CLOCK'  ->  /RIPPLE CLOCK
74191 timing.to: RIPPLE CLOCK'  ->  /RIPPLE CLOCK
74191 timing.from: D/U'  ->  /D/U
74191 timing.to: RIPPLE CLOCK'  ->  /RIPPLE CLOCK
74191 timing.from: D/U'  ->  /D/U
74191 timing.to: RIPPLE CLOCK'  ->  /RIPPLE CLOCK
74191 timing.from: D/U'  ->  /D/U
74191 timing.from: D/U'  ->  /D/U
74191 timingLimits.pin: LOAD'  ->  /LOAD
74191 timingLimits.ref: LOAD'  ->  /LOAD
74191 timingLimits.ref: LOAD'  ->  /LOAD
74191 outputs[5].pin: RIPPLE CLOCK'  ->  /RIPPLE CLOCK
74191 outputs[5].formula: RIPPLE CLOCK' = NOT (MAXMIN AND NOT G' AND NOT CLOCK)  ->  /RIPPLE CLOCK = NOT (MAXMIN AND NOT /G AND NOT CLOCK)
74191 internal[0]: MAXMIN = (NOT D/U' AND FA AND FB AND FC AND FD) OR (D/U' AND NOT FA AND NOT FB AND NOT FC AND NOT FD)  ->  MAXMIN = (NOT /D/U AND FA AND FB AND FC AND FD) OR (/D/U AND NOT FA AND NOT FB AND NOT FC AND NOT FD)
74191 stored[0].next: FA = FA XOR (G' AND ((D/U' AND 1) OR (NOT D/U' AND 1)))  ->  FA = FA XOR (/G AND ((/D/U AND 1) OR (NOT /D/U AND 1)))
74191 stored[0].async[0].when: NOT LOAD'  ->  NOT /LOAD
74191 stored[1].next: FB = FB XOR (G' AND ((D/U' AND NOT FA) OR (NOT D/U' AND FA)))  ->  FB = FB XOR (/G AND ((/D/U AND NOT FA) OR (NOT /D/U AND FA)))
74191 stored[1].async[0].when: NOT LOAD'  ->  NOT /LOAD
74191 stored[2].next: FC = FC XOR (G' AND ((D/U' AND NOT FA AND NOT FB) OR (NOT D/U' AND FA AND FB)))  ->  FC = FC XOR (/G AND ((/D/U AND NOT FA AND NOT FB) OR (NOT /D/U AND FA AND FB)))
74191 stored[2].async[0].when: NOT LOAD'  ->  NOT /LOAD
74191 stored[3].next: FD = FD XOR (G' AND ((D/U' AND NOT FA AND NOT FB AND NOT FC) OR (NOT D/U' AND FA AND FB AND FC)))  ->  FD = FD XOR (/G AND ((/D/U AND NOT FA AND NOT FB AND NOT FC) OR (NOT /D/U AND FA AND FB AND FC)))
74191 stored[3].async[0].when: NOT LOAD'  ->  NOT /LOAD
74193 pin 11: name LOAD'  ->  LOAD, inverted
74193 pin 12: name CARRY'  ->  CARRY, inverted
74193 pin 13: name BORROW'  ->  BORROW, inverted
74193 functions[1].formula: CARRY' = NOT (Q = 15 AND UP = L)  ->  /CARRY = NOT (Q = 15 AND UP = L)
74193 functions[2].formula: BORROW' = NOT (Q = 0 AND DOWN = L)  ->  /BORROW = NOT (Q = 0 AND DOWN = L)
74193 timing.to: CARRY'  ->  /CARRY
74193 timing.to: CARRY'  ->  /CARRY
74193 timing.to: BORROW'  ->  /BORROW
74193 timing.to: BORROW'  ->  /BORROW
74193 timing.from: LOAD'  ->  /LOAD
74193 timing.from: LOAD'  ->  /LOAD
74193 outputs[4].pin: CARRY'  ->  /CARRY
74193 outputs[4].formula: CARRY' = NOT (FA AND FB AND FC AND FD AND NOT UP)  ->  /CARRY = NOT (FA AND FB AND FC AND FD AND NOT UP)
74193 outputs[5].pin: BORROW'  ->  /BORROW
74193 outputs[5].formula: BORROW' = NOT (NOT FA AND NOT FB AND NOT FC AND NOT FD AND NOT DOWN)  ->  /BORROW = NOT (NOT FA AND NOT FB AND NOT FC AND NOT FD AND NOT DOWN)
74193 stored[0].async[1].when: NOT LOAD'  ->  NOT /LOAD
74193 stored[1].async[1].when: NOT LOAD'  ->  NOT /LOAD
74193 stored[2].async[1].when: NOT LOAD'  ->  NOT /LOAD
74193 stored[3].async[1].when: NOT LOAD'  ->  NOT /LOAD
74195 pin 1: name CLEAR'  ->  CLEAR, inverted
74195 pin 3: name K'  ->  K, inverted
74195 pin 9: name SHIFT/LOAD'  ->  SHIFT/LOAD, inverted
74195 pin 11: name QD'  ->  QD, inverted
74195 functions[1].formula: QD' = NOT QD  ->  /QD = NOT QD
74195 timing.from: CLEAR'  ->  /CLEAR
74195 timingLimits.pin: CLEAR'  ->  /CLEAR
74195 timingLimits.pin: SHIFT/LOAD'  ->  /SHIFT/LOAD
74195 timingLimits.pin: SHIFT/LOAD'  ->  /SHIFT/LOAD
74195 outputs[4].pin: QD'  ->  /QD
74195 outputs[4].formula: QD' = NOT FD  ->  /QD = NOT FD
74195 stored[0].next: FA = (NOT SHIFT/LOAD' AND A) OR (SHIFT/LOAD' AND ((J AND NOT FA) OR (K' AND FA)))  ->  FA = (NOT /SHIFT/LOAD AND A) OR (/SHIFT/LOAD AND ((J AND NOT FA) OR (/K AND FA)))
74195 stored[0].clear.pin: CLEAR'  ->  /CLEAR
74195 stored[1].next: FB = (NOT SHIFT/LOAD' AND B) OR (SHIFT/LOAD' AND (FA))  ->  FB = (NOT /SHIFT/LOAD AND B) OR (/SHIFT/LOAD AND (FA))
74195 stored[1].clear.pin: CLEAR'  ->  /CLEAR
74195 stored[2].next: FC = (NOT SHIFT/LOAD' AND C) OR (SHIFT/LOAD' AND (FB))  ->  FC = (NOT /SHIFT/LOAD AND C) OR (/SHIFT/LOAD AND (FB))
74195 stored[2].clear.pin: CLEAR'  ->  /CLEAR
74195 stored[3].next: FD = (NOT SHIFT/LOAD' AND D) OR (SHIFT/LOAD' AND (FC))  ->  FD = (NOT /SHIFT/LOAD AND D) OR (/SHIFT/LOAD AND (FC))
74195 stored[3].clear.pin: CLEAR'  ->  /CLEAR
74198 pin 13: name CLEAR'  ->  CLEAR, inverted
74198 timing.from: CLEAR'  ->  /CLEAR
74198 stored[0].clear.pin: CLEAR'  ->  /CLEAR
74198 stored[1].clear.pin: CLEAR'  ->  /CLEAR
74198 stored[2].clear.pin: CLEAR'  ->  /CLEAR
74198 stored[3].clear.pin: CLEAR'  ->  /CLEAR
74198 stored[4].clear.pin: CLEAR'  ->  /CLEAR
74198 stored[5].clear.pin: CLEAR'  ->  /CLEAR
74198 stored[6].clear.pin: CLEAR'  ->  /CLEAR
74198 stored[7].clear.pin: CLEAR'  ->  /CLEAR
74199 pin 1: name K'  ->  K, inverted
74199 pin 23: name SHIFT/LOAD'  ->  SHIFT/LOAD, inverted
74199 pin 14: name CLEAR'  ->  CLEAR, inverted
74199 timing.from: CLEAR'  ->  /CLEAR
74199 timingLimits.pin: SHIFT/LOAD'  ->  /SHIFT/LOAD
74199 stored[0].next: FA = (NOT SHIFT/LOAD' AND A) OR (SHIFT/LOAD' AND ((J AND NOT FA) OR (K' AND FA)))  ->  FA = (NOT /SHIFT/LOAD AND A) OR (/SHIFT/LOAD AND ((J AND NOT FA) OR (/K AND FA)))
74199 stored[0].clear.pin: CLEAR'  ->  /CLEAR
74199 stored[1].next: FB = (NOT SHIFT/LOAD' AND B) OR (SHIFT/LOAD' AND (FA))  ->  FB = (NOT /SHIFT/LOAD AND B) OR (/SHIFT/LOAD AND (FA))
74199 stored[1].clear.pin: CLEAR'  ->  /CLEAR
74199 stored[2].next: FC = (NOT SHIFT/LOAD' AND C) OR (SHIFT/LOAD' AND (FB))  ->  FC = (NOT /SHIFT/LOAD AND C) OR (/SHIFT/LOAD AND (FB))
74199 stored[2].clear.pin: CLEAR'  ->  /CLEAR
74199 stored[3].next: FD = (NOT SHIFT/LOAD' AND D) OR (SHIFT/LOAD' AND (FC))  ->  FD = (NOT /SHIFT/LOAD AND D) OR (/SHIFT/LOAD AND (FC))
74199 stored[3].clear.pin: CLEAR'  ->  /CLEAR
74199 stored[4].next: FE = (NOT SHIFT/LOAD' AND E) OR (SHIFT/LOAD' AND (FD))  ->  FE = (NOT /SHIFT/LOAD AND E) OR (/SHIFT/LOAD AND (FD))
74199 stored[4].clear.pin: CLEAR'  ->  /CLEAR
74199 stored[5].next: FF = (NOT SHIFT/LOAD' AND F) OR (SHIFT/LOAD' AND (FE))  ->  FF = (NOT /SHIFT/LOAD AND F) OR (/SHIFT/LOAD AND (FE))
74199 stored[5].clear.pin: CLEAR'  ->  /CLEAR
74199 stored[6].next: FG = (NOT SHIFT/LOAD' AND G) OR (SHIFT/LOAD' AND (FF))  ->  FG = (NOT /SHIFT/LOAD AND G) OR (/SHIFT/LOAD AND (FF))
74199 stored[6].clear.pin: CLEAR'  ->  /CLEAR
74199 stored[7].next: FH = (NOT SHIFT/LOAD' AND H) OR (SHIFT/LOAD' AND (FG))  ->  FH = (NOT /SHIFT/LOAD AND H) OR (/SHIFT/LOAD AND (FG))
74199 stored[7].clear.pin: CLEAR'  ->  /CLEAR
74200 pin 3: name ME1'  ->  ME1, inverted
74200 pin 4: name ME2'  ->  ME2, inverted
74200 pin 5: name ME3'  ->  ME3, inverted
74200 pin 12: name WE'  ->  WE, inverted
74200 timing.from: WE'  ->  /WE
74200 timing.from: WE'  ->  /WE
74200 timing.from: WE'  ->  /WE
74200 timing.from: WE'  ->  /WE
74200 timingLimits.pin: WE'  ->  /WE
74200 timingLimits.ref: WE'  ->  /WE
74200 timingLimits.ref: WE'  ->  /WE
74200 timingLimits.ref: WE'  ->  /WE
74200 timingLimits.ref: WE'  ->  /WE
74200 timingLimits.ref: WE'  ->  /WE
74200 timingLimits.ref: WE'  ->  /WE
74200 outputs[0].outputEnable.pin: ME1'  ->  /ME1
74200 outputs[0].outputEnable.pin: ME2'  ->  /ME2
74200 outputs[0].outputEnable.pin: ME3'  ->  /ME3
74200 outputs[0].outputEnable.pin: WE'  ->  /WE
74200 memory.write.when: NOT ME1' AND NOT ME2' AND NOT ME3' AND NOT WE'  ->  NOT /ME1 AND NOT /ME2 AND NOT /ME3 AND NOT /WE
74221 pin 3: name 1CLR'  ->  1CLR, inverted
74221 pin 4: name 1Q'  ->  1Q, inverted
74221 pin 11: name 2CLR'  ->  2CLR, inverted
74221 pin 12: name 2Q'  ->  2Q, inverted
74221 outputs[1].pin: 1Q'  ->  /1Q
74221 outputs[1].formula: 1Q' = NOT P1  ->  /1Q = NOT P1
74221 outputs[3].pin: 2Q'  ->  /2Q
74221 outputs[3].formula: 2Q' = NOT P2  ->  /2Q = NOT P2
74221 stored[0].clock.when: NOT 1A AND 1B AND 1CLR'  ->  NOT 1A AND 1B AND /1CLR
74221 stored[0].clear.pin: 1CLR'  ->  /1CLR
74221 stored[1].clock.when: NOT 2A AND 2B AND 2CLR'  ->  NOT 2A AND 2B AND /2CLR
74221 stored[1].clear.pin: 2CLR'  ->  /2CLR
74279 pin 1: name 1R'  ->  1R, inverted
74279 pin 2: name 1S1'  ->  1S1, inverted
74279 pin 3: name 1S2'  ->  1S2, inverted
74279 pin 5: name 2R'  ->  2R, inverted
74279 pin 6: name 2S'  ->  2S, inverted
74279 pin 10: name 3R'  ->  3R, inverted
74279 pin 11: name 3S1'  ->  3S1, inverted
74279 pin 12: name 3S2'  ->  3S2, inverted
74279 pin 14: name 4R'  ->  4R, inverted
74279 pin 15: name 4S'  ->  4S, inverted
74279 stored[0].next: L1 = NOT (1S1' AND 1S2') OR (L1 AND 1R')  ->  L1 = NOT (/1S1 AND /1S2) OR (L1 AND /1R)
74279 stored[1].next: L2 = NOT (2S') OR (L2 AND 2R')  ->  L2 = NOT (/2S) OR (L2 AND /2R)
74279 stored[2].next: L3 = NOT (3S1' AND 3S2') OR (L3 AND 3R')  ->  L3 = NOT (/3S1 AND /3S2) OR (L3 AND /3R)
74279 stored[3].next: L4 = NOT (4S') OR (L4 AND 4R')  ->  L4 = NOT (/4S) OR (L4 AND /4R)
7450 pin 12: name X'  ->  X, inverted
7453 pin 12: name X'  ->  X, inverted
7470 pin 5: name J'  ->  J, inverted
7470 pin 6: name Q'  ->  Q, inverted
7470 pin 9: name K'  ->  K, inverted
7470 functions[1].formula: Q' = NOT state  ->  /Q = NOT state
7470 timing.to: Q'  ->  /Q
7470 timing.to: Q'  ->  /Q
7470 timing.to: Q'  ->  /Q
7470 timing.to: Q'  ->  /Q
7470 outputs[1].pin: Q'  ->  /Q
7470 outputs[1].formula: Q' = NOT FF1 AND (PR OR CLR)  ->  /Q = NOT FF1 AND (PR OR CLR)
7470 stored[0].next: FF1 = (J1 AND J2 AND NOT J' AND NOT FF1) OR (NOT (K1 AND K2 AND NOT K') AND FF1)  ->  FF1 = (J1 AND J2 AND NOT /J AND NOT FF1) OR (NOT (K1 AND K2 AND NOT /K) AND FF1)
7472 pin 6: name Q'  ->  Q, inverted
7472 functions[1].formula: Q' = NOT state  ->  /Q = NOT state
7472 timing.to: Q'  ->  /Q
7472 timing.to: Q'  ->  /Q
7472 timing.to: Q'  ->  /Q
7472 timing.to: Q'  ->  /Q
7472 outputs[1].pin: Q'  ->  /Q
7472 outputs[1].formula: Q' = NOT FF1 OR (PR' AND CLR')  ->  /Q = NOT FF1 OR (PR' AND CLR')
7473 pin 8: name 2Q'  ->  2Q, inverted
7473 pin 13: name 1Q'  ->  1Q, inverted
7473 functions[1].formula: 1Q' = NOT state1  ->  /1Q = NOT state1
7473 functions[3].formula: 2Q' = NOT state2  ->  /2Q = NOT state2
7473 timing.to: 1Q'  ->  /1Q
7473 timing.to: 1Q'  ->  /1Q
7473 timing.to: 1Q'  ->  /1Q
7473 timing.to: 2Q'  ->  /2Q
7473 timing.to: 2Q'  ->  /2Q
7473 timing.to: 2Q'  ->  /2Q
7473 outputs[1].pin: 1Q'  ->  /1Q
7473 outputs[1].formula: 1Q' = NOT FF1  ->  /1Q = NOT FF1
7473 outputs[3].pin: 2Q'  ->  /2Q
7473 outputs[3].formula: 2Q' = NOT FF2  ->  /2Q = NOT FF2
7474 pin 6: name 1Q'  ->  1Q, inverted
7474 pin 8: name 2Q'  ->  2Q, inverted
7474 functions[1].formula: 1Q' = NOT state1  ->  /1Q = NOT state1
7474 functions[3].formula: 2Q' = NOT state2  ->  /2Q = NOT state2
7474 timing.to: 1Q'  ->  /1Q
7474 timing.to: 1Q'  ->  /1Q
7474 timing.to: 1Q'  ->  /1Q
7474 timing.to: 1Q'  ->  /1Q
7474 timing.to: 2Q'  ->  /2Q
7474 timing.to: 2Q'  ->  /2Q
7474 timing.to: 2Q'  ->  /2Q
7474 timing.to: 2Q'  ->  /2Q
7474 outputs[1].pin: 1Q'  ->  /1Q
7474 outputs[1].formula: 1Q' = NOT FF1 OR (1PR' AND 1CLR')  ->  /1Q = NOT FF1 OR (1PR' AND 1CLR')
7474 outputs[3].pin: 2Q'  ->  /2Q
7474 outputs[3].formula: 2Q' = NOT FF2 OR (2PR' AND 2CLR')  ->  /2Q = NOT FF2 OR (2PR' AND 2CLR')
7475 pin 1: name 1Q'  ->  1Q, inverted
7475 pin 8: name 4Q'  ->  4Q, inverted
7475 pin 11: name 3Q'  ->  3Q, inverted
7475 pin 14: name 2Q'  ->  2Q, inverted
7475 functions[1].formula: 1Q' = NOT state1  ->  /1Q = NOT state1
7475 functions[3].formula: 2Q' = NOT state2  ->  /2Q = NOT state2
7475 functions[5].formula: 3Q' = NOT state3  ->  /3Q = NOT state3
7475 functions[7].formula: 4Q' = NOT state4  ->  /4Q = NOT state4
7475 timing.to: 1Q'  ->  /1Q
7475 timing.to: 1Q'  ->  /1Q
7475 timing.to: 1Q'  ->  /1Q
7475 timing.to: 1Q'  ->  /1Q
7475 timing.to: 2Q'  ->  /2Q
7475 timing.to: 2Q'  ->  /2Q
7475 timing.to: 2Q'  ->  /2Q
7475 timing.to: 2Q'  ->  /2Q
7475 timing.to: 3Q'  ->  /3Q
7475 timing.to: 3Q'  ->  /3Q
7475 timing.to: 3Q'  ->  /3Q
7475 timing.to: 3Q'  ->  /3Q
7475 timing.to: 4Q'  ->  /4Q
7475 timing.to: 4Q'  ->  /4Q
7475 timing.to: 4Q'  ->  /4Q
7475 timing.to: 4Q'  ->  /4Q
7475 outputs[1].pin: 1Q'  ->  /1Q
7475 outputs[1].formula: 1Q' = NOT L1  ->  /1Q = NOT L1
7475 outputs[3].pin: 2Q'  ->  /2Q
7475 outputs[3].formula: 2Q' = NOT L2  ->  /2Q = NOT L2
7475 outputs[5].pin: 3Q'  ->  /3Q
7475 outputs[5].formula: 3Q' = NOT L3  ->  /3Q = NOT L3
7475 outputs[7].pin: 4Q'  ->  /4Q
7475 outputs[7].formula: 4Q' = NOT L4  ->  /4Q = NOT L4
7476 pin 10: name 2Q'  ->  2Q, inverted
7476 pin 14: name 1Q'  ->  1Q, inverted
7476 functions[1].formula: 1Q' = NOT state1  ->  /1Q = NOT state1
7476 functions[3].formula: 2Q' = NOT state2  ->  /2Q = NOT state2
7476 timing.to: 1Q'  ->  /1Q
7476 timing.to: 1Q'  ->  /1Q
7476 timing.to: 1Q'  ->  /1Q
7476 timing.to: 1Q'  ->  /1Q
7476 timing.to: 2Q'  ->  /2Q
7476 timing.to: 2Q'  ->  /2Q
7476 timing.to: 2Q'  ->  /2Q
7476 timing.to: 2Q'  ->  /2Q
7476 outputs[1].pin: 1Q'  ->  /1Q
7476 outputs[1].formula: 1Q' = NOT FF1 OR (1PR' AND 1CLR')  ->  /1Q = NOT FF1 OR (1PR' AND 1CLR')
7476 outputs[3].pin: 2Q'  ->  /2Q
7476 outputs[3].formula: 2Q' = NOT FF2 OR (2PR' AND 2CLR')  ->  /2Q = NOT FF2 OR (2PR' AND 2CLR')
74H55 pin 9: name X'  ->  X, inverted
74S112 pin 6: name 1Q'  ->  1Q, inverted
74S112 pin 7: name 2Q'  ->  2Q, inverted
74S112 functions[1].formula: 1Q' = NOT FF1  ->  /1Q = NOT FF1
74S112 functions[3].formula: 2Q' = NOT FF2  ->  /2Q = NOT FF2
74S112 outputs[1].pin: 1Q'  ->  /1Q
74S112 outputs[1].formula: 1Q' = NOT FF1 OR (1PR' AND 1CLR')  ->  /1Q = NOT FF1 OR (1PR' AND 1CLR')
74S112 outputs[3].pin: 2Q'  ->  /2Q
74S112 outputs[3].formula: 2Q' = NOT FF2 OR (2PR' AND 2CLR')  ->  /2Q = NOT FF2 OR (2PR' AND 2CLR')
74S114 pin 6: name 1Q'  ->  1Q, inverted
74S114 pin 8: name 2Q'  ->  2Q, inverted
74S114 functions[1].formula: 1Q' = NOT FF1  ->  /1Q = NOT FF1
74S114 functions[3].formula: 2Q' = NOT FF2  ->  /2Q = NOT FF2
74S114 outputs[1].pin: 1Q'  ->  /1Q
74S114 outputs[1].formula: 1Q' = NOT FF1 OR (1PR' AND CLR')  ->  /1Q = NOT FF1 OR (1PR' AND CLR')
74S114 outputs[3].pin: 2Q'  ->  /2Q
74S114 outputs[3].formula: 2Q' = NOT FF2 OR (2PR' AND CLR')  ->  /2Q = NOT FF2 OR (2PR' AND CLR')
74S258 pin 15: name G'  ->  G, inverted
74S258 functions[0].formula: nY = NOT ((SELECT' AND nA) OR (SELECT AND nB)), Z when G' high  ->  nY = NOT ((SELECT' AND nA) OR (SELECT AND nB)), Z when /G high
74S258 timing.from: G'  ->  /G
74S258 timing.from: G'  ->  /G
74S258 timing.from: G'  ->  /G
74S258 timing.from: G'  ->  /G
74S258 outputs[0].outputEnable.pin: G'  ->  /G
74S258 outputs[1].outputEnable.pin: G'  ->  /G
74S258 outputs[2].outputEnable.pin: G'  ->  /G
74S258 outputs[3].outputEnable.pin: G'  ->  /G
8095 pin 1: name G1'  ->  G1, inverted
8095 pin 15: name G2'  ->  G2, inverted
8095 functions[0].formula: Y = A when G1' = L AND G2' = L; Z when G1' = H OR G2' = H  ->  Y = A when /G1 = L AND /G2 = L; Z when /G1 = H OR /G2 = H
8095 outputs[0].outputEnable.pin: G1'  ->  /G1
8095 outputs[0].outputEnable.pin: G2'  ->  /G2
8095 outputs[1].outputEnable.pin: G1'  ->  /G1
8095 outputs[1].outputEnable.pin: G2'  ->  /G2
8095 outputs[2].outputEnable.pin: G1'  ->  /G1
8095 outputs[2].outputEnable.pin: G2'  ->  /G2
8095 outputs[3].outputEnable.pin: G1'  ->  /G1
8095 outputs[3].outputEnable.pin: G2'  ->  /G2
8095 outputs[4].outputEnable.pin: G1'  ->  /G1
8095 outputs[4].outputEnable.pin: G2'  ->  /G2
8095 outputs[5].outputEnable.pin: G1'  ->  /G1
8095 outputs[5].outputEnable.pin: G2'  ->  /G2
8096 pin 1: name G1'  ->  G1, inverted
8096 pin 15: name G2'  ->  G2, inverted
8096 functions[0].formula: Y = NOT A when G1' = L AND G2' = L; Z when G1' = H OR G2' = H  ->  Y = NOT A when /G1 = L AND /G2 = L; Z when /G1 = H OR /G2 = H
8096 outputs[0].outputEnable.pin: G1'  ->  /G1
8096 outputs[0].outputEnable.pin: G2'  ->  /G2
8096 outputs[1].outputEnable.pin: G1'  ->  /G1
8096 outputs[1].outputEnable.pin: G2'  ->  /G2
8096 outputs[2].outputEnable.pin: G1'  ->  /G1
8096 outputs[2].outputEnable.pin: G2'  ->  /G2
8096 outputs[3].outputEnable.pin: G1'  ->  /G1
8096 outputs[3].outputEnable.pin: G2'  ->  /G2
8096 outputs[4].outputEnable.pin: G1'  ->  /G1
8096 outputs[4].outputEnable.pin: G2'  ->  /G2
8096 outputs[5].outputEnable.pin: G1'  ->  /G1
8096 outputs[5].outputEnable.pin: G2'  ->  /G2
```
