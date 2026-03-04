# Polygon Editor

Interactive **polygon editor** implemented in **C# (Windows Forms)** as part of a Computer Graphics course project.

The application allows users to create and edit polygons interactively while supporting **geometric constraints**, **Bezier curves**, and different **line rendering algorithms**.

The project was designed with emphasis on **clean architecture (MVC)** and modular extensibility.

---

# Features

### Polygon editing

* Create and edit polygons interactively
* Move vertices and edges
* Insert new vertices
* Remove vertices

### Edge types

Edges can be represented as:

* straight line segments
* circular arcs
* **Bezier curves**

### Geometric constraints

The editor allows applying constraints to polygon edges:

* parallel edges
* fixed edge length
* vertex continuity classes

Constraints are automatically maintained when vertices are moved.

### Rendering algorithms

Two rendering methods are available:

* standard library drawing
* **Bresenham line algorithm**

---

# User Interaction

The application is controlled mainly with the **mouse**.

### Moving elements

Using the **left mouse button (LMB)**:

* move polygon vertices
* move Bezier control points

### Context menu

Using the **right mouse button (RMB)**:

* open context menu for vertices
* open context menu for edges

For Bezier curves or circular arcs, the context menu can be opened by clicking the **dashed segment between vertices**.

---

# Editing Operations

Available editing actions include:

* Add vertex (in the middle of an edge)
* Remove vertex
* Set vertex continuity class
* Add geometric constraint
* Remove constraints
* Convert edge to circular arc
* Convert edge to Bezier curve

---

# Interface

The control panel above the scene provides:

### Rendering options

Two radio buttons allow switching between rendering algorithms:

* default rendering
* Bresenham algorithm

### Help

Displays application usage instructions.

### Reset

Clears the scene and restores the initial polygon.

---

# Architecture

The application is built using the **MVC (Model–View–Controller)** architectural pattern.

### Model

Represents the data structures:

* polygons
* vertices
* edges
* geometric constraints

### View

Rendering is handled by **SceneView**, which interprets the model and draws the scene.

Drawing algorithms implement a common interface:

```id="edge_drawer_interface"
IEdgeDrawer
```

Different drawing strategies can be swapped dynamically.

### Controller

**SceneController** manages:

* user interaction
* model updates
* triggering rendering operations

---

# Constraint System

Constraints are implemented as objects that implement the **Constraint interface**.

Each constraint defines a strategy for correcting vertex positions when the polygon structure changes.

When a vertex moves:

1. `SceneController` detects the change
2. the corresponding polygon is updated
3. `ConstraintSolver` adjusts vertices to maintain constraints
4. constraint propagation ensures the polygon remains consistent

Before adding a constraint, the solver validates whether the constraint is compatible with the current structure.

---

# Continuity Handling

Vertex continuity is handled by the **ContinuitiesSolver**.

Different continuity classes enforce different geometric relationships between connected edges.

---

# Rendering System

Rendering is performed by **SceneView**.

The controller selects the rendering strategy, which is then used to draw edges on screen.

Available implementations include:

* library drawing
* Bresenham algorithm implementation

---

# Technologies

* **C#**
* **Windows Forms**
* Object-Oriented Programming
* MVC architecture
* Computer Graphics algorithms

---

# Author

Filip Sewastianowicz

Computer Science Student
Warsaw University of Technology

---

# License

This project was created for **educational purposes** as part of a university Computer Graphics course.
