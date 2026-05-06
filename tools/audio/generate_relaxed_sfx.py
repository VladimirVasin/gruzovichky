from __future__ import annotations

import argparse
import hashlib
import math
import struct
import wave
from pathlib import Path

import numpy as np

SR = 44100


def stable_seed(text: str) -> int:
    return int(hashlib.md5(text.encode("utf-8")).hexdigest()[:8], 16)


def t(duration: float) -> np.ndarray:
    return np.arange(int(math.ceil(duration * SR)), dtype=np.float64) / SR


def smoothstep(x: np.ndarray) -> np.ndarray:
    x = np.clip(x, 0.0, 1.0)
    return x * x * (3.0 - 2.0 * x)


def env_ar(tt: np.ndarray, attack: float, release: float) -> np.ndarray:
    return smoothstep(tt / max(attack, 1e-4)) * np.exp(-release * np.maximum(0.0, tt - attack))


def gentle_noise(name: str, n: int, amount: float = 1.0, smooth: float = 0.995) -> np.ndarray:
    rng = np.random.default_rng(stable_seed(name))
    raw = rng.normal(0.0, 1.0, n)
    out = np.zeros(n, dtype=np.float64)
    acc = 0.0
    feed = 1.0 - smooth
    for i, v in enumerate(raw):
        acc = acc * smooth + v * feed
        out[i] = acc
    peak = np.max(np.abs(out))
    if peak > 1e-8:
        out /= peak
    return out * amount


def normalize(samples: np.ndarray, peak: float = 0.68) -> np.ndarray:
    samples = np.asarray(samples, dtype=np.float64)
    samples -= float(np.mean(samples))
    p = float(np.max(np.abs(samples))) if samples.size else 0.0
    if p > 1e-8:
        samples = samples / p * peak
    return np.clip(samples, -1.0, 1.0)


def write_wav(path: Path, samples: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = (np.clip(samples, -1.0, 1.0) * 32767.0).astype("<i2")
    with wave.open(str(path), "wb") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SR)
        wf.writeframes(pcm.tobytes())


def guid_for(path: Path) -> str:
    return hashlib.md5(path.as_posix().lower().encode("utf-8")).hexdigest()


def write_folder_meta(folder: Path) -> None:
    folder.mkdir(parents=True, exist_ok=True)
    meta = folder.with_name(folder.name + ".meta")
    if meta.exists():
        return
    meta.write_text(
        f"""fileFormatVersion: 2
guid: {guid_for(folder)}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )


def write_audio_meta(wav: Path) -> None:
    wav.with_name(wav.name + ".meta").write_text(
        f"""fileFormatVersion: 2
guid: {guid_for(wav)}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 0
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )


def soft_click(name: str, freq: float, duration: float, amp: float) -> np.ndarray:
    tt = t(duration)
    e = env_ar(tt, 0.018, 11.0)
    warm = freq * 0.72
    body = np.sin(2 * np.pi * warm * tt) * 0.58
    body += np.sin(2 * np.pi * warm * 1.5 * tt + 0.45) * 0.18
    tap = np.sin(2 * np.pi * 86.0 * tt) * np.exp(-24.0 * tt) * 0.22
    return normalize((body + tap) * e, amp)


def mallet(name: str, freqs: list[float], starts: list[float], duration: float, amp: float) -> np.ndarray:
    tt = t(duration)
    out = np.zeros_like(tt)
    rng = np.random.default_rng(stable_seed(name))
    for freq, start in zip(freqs, starts):
        local = tt - start
        active = local >= 0.0
        lt = local[active]
        detune = 1.0 + rng.uniform(-0.0025, 0.0025)
        e = env_ar(lt, 0.018, 5.6)
        f = freq * detune
        tone = np.sin(2 * np.pi * f * lt) * 0.48
        tone += np.sin(2 * np.pi * f * 1.5 * lt + 0.35) * 0.09
        tone += np.sin(2 * np.pi * 94.0 * lt) * np.exp(-14.0 * lt) * 0.06
        out[active] += tone * e
    return normalize(out, amp)


def wood_thunk(name: str, duration: float, amp: float, base: float) -> np.ndarray:
    tt = t(duration)
    impact = np.exp(-24.0 * tt)
    tail = np.exp(-7.0 * tt)
    soft_noise = gentle_noise(name, tt.size, 0.08, 0.94) * tail
    s = np.sin(2 * np.pi * base * tt) * impact * 0.58
    s += np.sin(2 * np.pi * base * 1.62 * tt + 0.6) * impact * 0.16
    s += np.sin(2 * np.pi * 245.0 * tt + 0.2) * tail * 0.05
    return normalize(s + soft_noise, amp)


def engine_loop(name: str, duration: float, amp: float, rpm: float, motion: float) -> np.ndarray:
    tt = t(duration)
    noise = gentle_noise(name, tt.size, 0.08 + motion * 0.06, 0.997)
    wobble = 0.88 + np.sin(2 * np.pi * 0.42 * tt) * 0.06 + np.sin(2 * np.pi * 1.15 * tt + 0.4) * 0.02
    r = rpm + np.sin(2 * np.pi * 0.18 * tt) * (1.2 + motion * 2.0)
    s = np.sin(2 * np.pi * r * tt) * 0.36
    s += np.sin(2 * np.pi * r * 1.9 * tt + 0.2) * 0.10
    s += np.sin(2 * np.pi * r * 2.55 * tt + 0.8) * 0.024
    return normalize((s + noise) * wobble, amp)


def wind_loop(name: str, duration: float, amp: float, dark: float = 0.0) -> np.ndarray:
    tt = t(duration)
    bed = np.sin(2 * np.pi * 0.08 * tt) * 0.24 + np.sin(2 * np.pi * 0.15 * tt + 1.1) * 0.16
    air = gentle_noise(name, tt.size, 0.34 + dark * 0.08, 0.998)
    swell = 0.72 + np.sin(2 * np.pi * 0.035 * tt + 0.4) * 0.14
    return normalize((bed + air) * swell, amp)


def river_loop(name: str, duration: float, amp: float) -> np.ndarray:
    tt = t(duration)
    water = gentle_noise(name, tt.size, 0.24, 0.992)
    s = np.sin(2 * np.pi * 82.0 * tt + np.sin(2 * np.pi * 0.05 * tt)) * 0.16
    s += np.sin(2 * np.pi * 136.0 * tt + 1.3) * 0.08
    swell = 0.72 + np.sin(2 * np.pi * 0.07 * tt) * 0.12 + np.sin(2 * np.pi * 0.12 * tt + 1.4) * 0.07
    return normalize((s + water) * swell, amp)


def crickets(name: str, duration: float, amp: float) -> np.ndarray:
    tt = t(duration)
    pulse = np.maximum(0.0, np.sin(2 * np.pi * 2.05 * tt)) ** 12 * 0.18
    pulse += np.maximum(0.0, np.sin(2 * np.pi * 2.75 * tt + 1.0)) ** 14 * 0.10
    tone = np.sin(2 * np.pi * 1750.0 * tt) * 0.030 + np.sin(2 * np.pi * 2380.0 * tt + 0.2) * 0.012
    return normalize(tone * pulse, amp)


def hum_loop(name: str, duration: float, amp: float, base: float) -> np.ndarray:
    tt = t(duration)
    w = 0.78 + np.sin(2 * np.pi * 0.12 * tt) * 0.08
    s = np.sin(2 * np.pi * base * tt) * 0.32
    s += np.sin(2 * np.pi * base * 1.6 * tt + 0.5) * 0.10
    s += np.sin(2 * np.pi * base * 2.2 * tt + 1.1) * 0.025
    return normalize(s * w, amp)


def sparse_birds(name: str, duration: float, amp: float) -> np.ndarray:
    return mallet(name, [650.0, 735.0, 585.0], [0.7, 2.6, 5.0], duration, amp)


def splash(name: str, duration: float, amp: float) -> np.ndarray:
    tt = t(duration)
    water = gentle_noise(name, tt.size, 0.22, 0.86) * np.exp(-13.0 * tt)
    low = np.sin(2 * np.pi * 82.0 * tt) * np.exp(-30.0 * tt) * 0.22
    return normalize(water + low, amp)


def owl(name: str, duration: float, amp: float) -> np.ndarray:
    tt = t(duration)
    e = env_ar(tt, 0.08, 2.4)
    f = 285.0 - tt * 28.0
    s = np.sin(2 * np.pi * f * tt) * 0.18 + np.sin(2 * np.pi * f * 1.17 * tt + 0.2) * 0.05
    return normalize(s * e, amp)


def creak(name: str, duration: float, amp: float) -> np.ndarray:
    tt = t(duration)
    e = env_ar(tt, 0.03, 6.0)
    f = 96.0 + tt * 55.0 + np.sin(2 * np.pi * 2.2 * tt) * 10.0
    s = np.sin(2 * np.pi * f * tt) * 0.09 + np.sin(2 * np.pi * f * 1.5 * tt + 0.7) * 0.026
    return normalize(s * e, amp)


def buzz(name: str, duration: float, amp: float) -> np.ndarray:
    tt = t(duration)
    e = env_ar(tt, 0.015, 9.0)
    s = np.sin(2 * np.pi * 58.0 * tt) * 0.040 + np.sin(2 * np.pi * 116.0 * tt + 0.4) * 0.010
    return normalize(s * e, amp)


CLIPS = {
    "ui_select": lambda: soft_click("ui_select", 280.0, 0.18, 0.34),
    "menu_hover": lambda: soft_click("menu_hover", 356.0, 0.18, 0.26),
    "ui_open": lambda: soft_click("ui_open", 420.0, 0.18, 0.34),
    "ui_close": lambda: soft_click("ui_close", 220.0, 0.18, 0.30),
    "building_complete": lambda: mallet("building_complete", [293.66, 392.0, 523.25], [0.0, 0.08, 0.18], 0.65, 0.36),
    "road_drag": lambda: mallet("road_drag", [261.63, 329.63], [0.0, 0.06], 0.24, 0.26),
    "building_demolish": lambda: wood_thunk("building_demolish", 0.34, 0.34, 92.0),
    "truck_idle": lambda: engine_loop("truck_idle", 2.8, 0.24, 43.0, 0.0),
    "truck_roll": lambda: engine_loop("truck_roll", 2.0, 0.25, 72.0, 1.0),
    "boat_motor": lambda: engine_loop("boat_motor", 3.8, 0.20, 58.0, 0.25),
    "slot_reel_tick": lambda: soft_click("slot_reel_tick", 820.0, 0.08, 0.34),
    "slot_win": lambda: mallet("slot_win", [329.63, 392.0, 440.0, 523.25, 659.25], [0.0, 0.13, 0.27, 0.43, 0.61], 1.1, 0.42),
    "slot_lose": lambda: mallet("slot_lose", [523.25, 466.16, 392.0, 311.13], [0.0, 0.16, 0.34, 0.54], 0.85, 0.34),
    "tutorial_goal_success": lambda: mallet("tutorial_goal_success", [329.63, 392.0, 440.0, 523.25, 659.25], [0.0, 0.13, 0.27, 0.43, 0.61], 1.1, 0.38),
}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="Assets/Resources/GeneratedAudio/Relaxed")
    args = parser.parse_args()

    out = Path(args.output)
    write_folder_meta(out.parent)
    write_folder_meta(out)

    expected = {f"{name}.wav" for name in CLIPS}
    for stale in out.glob("*.wav"):
        if stale.name not in expected:
            stale.unlink()
            meta = stale.with_name(stale.name + ".meta")
            if meta.exists():
                meta.unlink()

    for name, factory in CLIPS.items():
        path = out / f"{name}.wav"
        write_wav(path, factory())
        write_audio_meta(path)
        print(f"generated {path}")


if __name__ == "__main__":
    main()
