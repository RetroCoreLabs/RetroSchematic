"""Mix-up check: 300 pieces drawn at random from all named groups, sorted by name, shown with
their group name on 2 sheets (harvest/check0.png, check1.png) to count wrong ones by eye.
Usage: python -I checksample.py <harvestdir>"""
import site, sys
sys.path.append(site.getusersitepackages())
import numpy as np, cv2
hd = sys.argv[1]
M = np.load(hd + "/pieces.npz")["M"]; a = np.load(hd + "/clusters.npz")["assign"]
nm = {int(l.split()[0]): l.split()[1] for l in open(hd + "/names.txt") if l[0] != "#" and l.strip()}
lab = np.array([nm.get(int(g), "") for g in a])
idx = np.nonzero(lab != "")[0]
s = np.random.default_rng(1).choice(idx, 300, replace=False)
s = s[np.argsort(lab[s], kind="stable")]
np.save(hd + "/check_idx.npy", s)
T = 48
for sh in range(2):
    img = np.full((10 * (T + 16), 15 * (T + 4)), 255, np.uint8)
    for j, q in enumerate(s[sh * 150:(sh + 1) * 150]):
        r, c = divmod(j, 15)
        img[r * (T + 16) + 14:r * (T + 16) + 14 + T, c * (T + 4):c * (T + 4) + T] = cv2.resize(255 - M[q], (T, T))
        cv2.putText(img, f"{sh*150+j}:{lab[q]}", (c * (T + 4), r * (T + 16) + 11), 0, 0.35, 0, 1)
    cv2.imwrite(hd + f"/check{sh}.png", img)
