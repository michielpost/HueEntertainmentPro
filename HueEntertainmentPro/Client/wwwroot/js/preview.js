//const previewConnection = new signalR.HubConnectionBuilder()
//    .withUrl("/previewhub")
//    .withAutomaticReconnect()
//    .build();

//previewConnection.start()
//  .catch(err => console.error(err.toString()));

var lights = [];
var cellSize = 0;
var allowEdit = false;
var lightContainer = new PIXI.Container();
var app; // Global reference to Pixi application
var container; // Global reference to container
var resizeHandler; // Global reference to the window resize listener

function placeLight(bridgeIp, lightId, x, y, hex, bri, groupId, positionIndex) {
  var bridgeArray = lights[bridgeIp];
  var rad = 30 * bri;
  var xPos = xyToPosition(x);
  var yPos = xyToPosition(y * -1);

  if (bridgeArray === undefined || bridgeArray == null) {
    lights[bridgeIp] = [];
  }

  var id = lightId + '_' + positionIndex;
  if (positionIndex === undefined || positionIndex == null) {
    id = lightId;
  }
  var current = lights[bridgeIp][id];

  if (current === undefined || current == null) {
    lights[bridgeIp][id] = {
      glow: createGlowRing(xPos, yPos),
      label: createLightLabel(xPos, yPos, id, bridgeIp),
      groupId: groupId,
      lightId: lightId,
      positionIndex: positionIndex
    };
    updateGlowRing(bridgeIp, id, hex, bri);
    addLightToContainer(lights[bridgeIp][id]);
  } else {
    updateGlowRing(bridgeIp, id, hex, bri);
    // Update positions on resize
    lights[bridgeIp][id].glow.position.set(xPos, yPos);
    lights[bridgeIp][id].label.position.set(xPos, yPos);
  }
}

function xyToPosition(coord) {
  const size = Math.min(container.clientWidth, container.clientHeight);
  return (size / 2 + coord * size / 2);
}

function renderPreviewGrid(allowEditParam) {
  allowEdit = false;
  container = document.getElementById("pixiPreview");

  // Clean up any previous preview session, so lights of a previous connection don't linger
  if (app) {
    try { app.destroy(true); } catch (e) { }
    app = null;
  }
  if (resizeHandler) {
    window.removeEventListener('resize', resizeHandler);
    resizeHandler = null;
  }
  lights = [];
  lightContainer = new PIXI.Container();

  // Create Pixi application with resizeTo option
  app = new PIXI.Application({
    resizeTo: container,
    backgroundColor: 0x191D21,
    antialias: true
  });
  container.appendChild(app.view);
  container.style.width = '100%';
  container.style.height = '100%';

  // Handle window resize
  resizeHandler = () => {
    const size = Math.min(container.clientWidth, container.clientHeight);
    cellSize = size / 20;
    app.renderer.resize(size, size); // Maintain square aspect ratio
    updateAllElements();
  };
  window.addEventListener('resize', resizeHandler);

  // Initial size calculation
  const size = Math.min(container.clientWidth, container.clientHeight);
  cellSize = size / 20;
  app.renderer.resize(size, size);

  // Create the stage
  app.stage = new PIXI.display.Stage();
  addGridLines(app.stage, allowEdit);
  app.stage.addChild(lightContainer);

  if (allowEdit) {
    document.getElementById("saveButton").onclick = saveLocations;
    //previewConnection.on("newLocations", (preview) => {
    //  for (var i = 0; i < preview.length; i++) {
    //    var light = preview[i];
    //    placeLight(light.bridge, light.id, light.x, light.y, light.hex, light.bri, light.groupId, light.positionIndex)
    //  }
    //});
  } else {
    app.renderer.plugins.interaction.on('pointerdown', touch);
    //previewConnection.on("preview", (preview) => {
    //  for (var i = 0; i < preview.length; i++) {
    //    var light = preview[i];
    //    placeLight(light.bridge, light.id, light.x, light.y, light.hex, light.bri)
    //  }
    //});
  }

  function addGridLines(stage, drawGrid) {
    var graphics = new PIXI.Graphics();
    var stepSize = cellSize * 2;
    const size = Math.min(container.clientWidth, container.clientHeight);

    graphics.lineStyle(1, 0xffffff, 1);
    graphics.moveTo(1, 0);
    graphics.lineTo(0, size);
    graphics.moveTo(0, 1);
    graphics.lineTo(size, 0);

    if (drawGrid) {
      var middle = size / 2;
      graphics.moveTo(0, 0);
      graphics.lineTo(size, size);
      graphics.moveTo(0, size);
      graphics.lineTo(size, 0);

      for (var i = stepSize; i <= size; i += stepSize) {
        graphics.moveTo(i, 0);
        graphics.lineTo(i, size);
        graphics.moveTo(middle, middle);
        graphics.lineTo(i, size);
        graphics.moveTo(middle, middle);
        graphics.lineTo(i, 0);
        graphics.moveTo(0, i);
        graphics.lineTo(size, i);
        graphics.moveTo(middle, middle);
        graphics.lineTo(size, i);
        graphics.moveTo(middle, middle);
        graphics.lineTo(0, i);
      }
    }

    graphics.moveTo(size - 1, 0);
    graphics.lineTo(size, size);
    graphics.moveTo(0, size - 1);
    graphics.lineTo(size, size - 1);

    stage.addChild(graphics);
  }

  function saveLocations() {
    var result = [];
    for (const [key, values] of Object.entries(lights)) {
      for (var i = 0; i < Object.keys(lights[key]).length; i++) {
        var prop = Object.keys(lights[key])[i];
        var l = lights[key][prop];
        if (l != undefined && l != null) {
          var pos = getXYPosition(l.label);
          result.push({
            Id: l.lightId,
            Bridge: key,
            GroupId: l.groupId,
            PositionIndex: l.positionIndex,
            X: pos.x,
            Y: pos.y
          });
        }
      }
    }
    //previewConnection.invoke("SetLocations", result).catch(err => console.error(err.toString()));
  }

  function touch(event) {
    var pos = event.data.getLocalPosition(app.stage);
    //previewConnection.invoke("touch", positionToXY(pos.x), positionToXY(app.renderer.height - pos.y)).catch(err => console.error(err.toString()));
    console.log('Pointer at: ' + positionToXY(pos.x) + ',' + positionToXY(app.renderer.height - pos.y));
  }

  function getXYPosition(obj) {
    const size = Math.min(container.clientWidth, container.clientHeight);
    return {
      x: positionToXY(obj.x),
      y: positionToXY(size - obj.y)
    };
  }

  function positionToXY(x) {
    const size = Math.min(container.clientWidth, container.clientHeight);
    return (x / (size / 2) - 1);
  }

  function updateAllElements() {
    for (const [bridgeIp, values] of Object.entries(lights)) {
      for (const [id, light] of Object.entries(values)) {
        const pos = getXYPosition(light.label);
        const xPos = xyToPosition(pos.x);
        const yPos = xyToPosition(pos.y * -1);
        light.glow.position.set(xPos, yPos);
        light.label.position.set(xPos, yPos);
        light.glow.width = cellSize * 5 * (light.glow.width / (cellSize * 5));
        light.glow.height = cellSize * 5 * (light.glow.height / (cellSize * 5));
      }
    }
  }
}

function addLightToContainer(light) {
  lightContainer.addChild(light.glow);
  lightContainer.addChild(light.label);

  if (allowEdit) {
    light.label.interactive = true;
    light.label.buttonMode = true;
    light.label.on('pointerdown', onDragStart)
      .on('pointerup', onDragEnd)
      .on('pointerupoutside', onDragEnd)
      .on('pointermove', onDragMove);
  }
}

function updateGlowRing(bridgeIp, id, hex, bri) {
  lights[bridgeIp][id].glow.tint = "0x" + hex;
  lights[bridgeIp][id].glow.height = cellSize * 5 * bri;
  lights[bridgeIp][id].glow.width = cellSize * 5 * bri;
}

function createGlowRing(x, y) {
  var glowRing = PIXI.Sprite.fromImage("../glow.png");
  glowRing.anchor.set(0.5, 0.5);
  glowRing.height = cellSize * 5;
  glowRing.width = cellSize * 5;
  glowRing.position.x = x;
  glowRing.position.y = y;
  glowRing.blendMode = PIXI.BLEND_MODES.SCREEN;
  return glowRing;
}

function createLightLabel(x, y, id, bridgeIp) {
  var textColor = 0xE9ECEF;
  if (allowEdit)
    textColor = 0xFF0000;

  var label = new PIXI.Text(String(id), { font: "12px", fill: textColor });
  label.anchor.set(0.5, 0.5);
  label.position.x = x;
  label.position.y = y;
  label.lightId = id;
  label.bridgeIp = bridgeIp;

  return label;
}

function onDragStart(event) {
  //previewConnection.invoke("Locate", { id: this.lightId, bridge: this.bridgeIp }).catch(err => console.error(err.toString()));
  this.data = event.data;
  this.alpha = 0.5;
  this.dragging = true;
}

function onDragEnd() {
  this.alpha = 1;
  this.dragging = false;
  this.data = null;
}

function onDragMove() {
  if (this.dragging) {
    var newPosition = this.data.getLocalPosition(this.parent);
    this.x = newPosition.x;
    this.y = newPosition.y;
    this.parent.getChildAt(this.parent.getChildIndex(this) - 1).position.set(newPosition.x, newPosition.y);
  }
}
