#!/usr/bin/env python3
import sys
import os

import numpy as np

import matplotlib.pyplot as plt 


def main():
    fp = sys.argv[1]
    names, admin_names, coords = [], [], []
    with open(fp) as handle:
        handle.readline()
        for line in handle:
            name, x, y, _, _, admin_name, *_ = line.strip().split(",")
            x, y = float(x), float(y)
            coords.append(np.array([x, y]))
            names.append(name)
            admin_names.append(admin_name)

    coords = np.array(coords)

    colors = ["r" if s == "Noord-Holland" else "k" for s in admin_names]
    colors[names.index("Amsterdam")] = "b"
    sizes = [1 for _ in names]
    sizes[names.index("Amsterdam")] = 25
    plt.scatter(coords[:, 0], coords[:, 1], s=sizes, c=colors)
    plt.xlabel("Latitude")
    plt.ylabel("Longitude")
    plt.savefig("../out/netherlands.png")
    plt.clf()


if __name__ == "__main__":
    main()