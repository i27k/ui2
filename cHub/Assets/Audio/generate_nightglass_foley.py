"""Deterministically generates original c/Hub mechanical Foley masters.

These are source masters for Unity import, not replacements for recordings from
other games. Standard-library only so the build workstation can reproduce them.
"""
import math, os, random, struct, wave

RATE = 48000
ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, "NightglassR01")
random.seed(270119)

def noise(): return random.uniform(-1.0, 1.0)

def env(t, attack, decay):
    return min(1.0, t / max(attack, 1e-5)) * math.exp(-t / max(decay, 1e-5))

def resonator(t, freq, decay, phase=0.0):
    return math.sin(2 * math.pi * freq * t + phase) * math.exp(-t / decay)

def write(name, duration, synth, gain=0.92):
    os.makedirs(OUT, exist_ok=True)
    samples = []
    peak = 1e-9
    for i in range(int(duration * RATE)):
        value = synth(i / RATE)
        samples.append(value); peak = max(peak, abs(value))
    scale = gain / peak
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as wav:
        wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(RATE)
        wav.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, x * scale)) * 32767)) for x in samples))

def impulse_train(t, moments, body=1.0):
    value = 0.0
    for at, strength, pitch in moments:
        d = t - at
        if d >= 0:
            value += strength * (0.48 * resonator(d, pitch, .035) +
                                 0.24 * resonator(d, pitch * 2.73, .018) +
                                 noise() * math.exp(-d / .012))
    return value * body

write("chub_ng_mag_release", .22, lambda t: impulse_train(t, [(0.012, .8, 1450), (.055, .35, 620)]))
write("chub_ng_mag_extract", .48, lambda t: impulse_train(t, [(0.02,.42,420),(.11,.65,185),(.27,.28,760)]) + noise()*env(t,.01,.18)*.10)
write("chub_ng_mag_grab", .34, lambda t: impulse_train(t, [(0.02,.24,260),(.09,.18,540),(.18,.20,340)]) + noise()*env(t,.02,.14)*.07)
write("chub_ng_mag_align", .30, lambda t: impulse_train(t, [(0.03,.18,910),(.14,.23,630)]) + noise()*env(t,.02,.12)*.06)
write("chub_ng_mag_insert", .44, lambda t: impulse_train(t, [(0.02,.25,330),(.17,.55,520),(.205,.85,195)]) + noise()*env(t,.01,.16)*.08)
write("chub_ng_mag_seat", .25, lambda t: impulse_train(t, [(0.018,.95,170),(.052,.32,780)]))
write("chub_ng_bolt_charge", .58, lambda t: impulse_train(t, [(0.02,.35,410),(.19,.45,220),(.36,.95,155),(.405,.40,960)]) + noise()*env(t,.01,.22)*.09)

def gunshot(t):
    crack = noise() * env(t, .0001, .022) * 1.4
    body = resonator(t, 74, .16) * .9 + resonator(t, 137, .10) * .45
    mech = impulse_train(t, [(0.006,.34,880),(.054,.22,320)])
    tail = noise() * math.exp(-t/.24) * .14
    return crack + body + mech + tail
write("chub_ng_fire_close", .72, gunshot, .88)

def distant(t):
    return resonator(t, 62, .42) * .75 + resonator(t, 108, .28) * .38 + noise()*env(t,.004,.32)*.16
write("chub_ng_fire_distant", 1.25, distant, .78)

# A reference composite for reviewing the intended reload rhythm.
events = [
    (0.00, "chub_ng_mag_release"), (0.17, "chub_ng_mag_extract"),
    (0.72, "chub_ng_mag_grab"), (1.08, "chub_ng_mag_align"),
    (1.34, "chub_ng_mag_insert"), (1.69, "chub_ng_mag_seat"),
    (2.04, "chub_ng_bolt_charge")]
clips = {}
for start, name in events:
    with wave.open(os.path.join(OUT, name + ".wav"), "rb") as wav:
        clips[name] = [struct.unpack("<h", wav.readframes(1))[0] / 32767.0 for _ in range(wav.getnframes())]
def composite(t):
    total = 0.0
    for start, name in events:
        index = int((t-start)*RATE)
        if 0 <= index < len(clips[name]): total += clips[name][index]
    return total
write("chub_ng_reload_reference", 2.75, composite, .82)
print("Generated", len(os.listdir(OUT)), "original WAV masters in", OUT)
