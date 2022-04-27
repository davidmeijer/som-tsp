[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)

---

### Description
Small console app for using self-organising maps to calculate a sub-optimal solution for the traveling salesman problem for a given set of coordinates.

### Example
Create a sub-optimal shortest route for visiting all places in The Netherlands:
```bash
./run_program.sh ./data/nl.csv
```

<table>
  <tr>
    <td>0 iterations</td>
    <td>5,000 iterations</td>
    <td>10,000 iterations</td>
  </tr>
  <tr>
    <td><img src="out/output_iter_0.png" width=200 height=125></td>
    <td><img src="out/output_iter_5000.png" width=200 height=125></td>
    <td><img src="out/output_iter_10000.png" width=200 height=125></td>
  </tr>
  <tr>
    <td>15,000 iterations</td>
    <td>20,000 iterations</td>
    <td>25,000 iterations</td>
  </tr>
  <tr>
    <td><img src="out/output_iter_15000.png" width=200 height=125></td>
    <td><img src="out/output_iter_20000.png" width=200 height=125></td>
    <td><img src="out/output_iter_25000.png" width=200 height=125></td>
  </tr>
 </table>