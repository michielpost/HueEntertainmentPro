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

var LERP_SPEED = 12; // easing speed toward target color/brightness (per second)

function placeLight(bridgeIp, lightId, x, y, hex, bri, groupId, positionIndex) {
  var bridgeArray = lights[bridgeIp];
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
    var light = {
      halo: createGlowSprite(xPos, yPos, PIXI.BLEND_MODES.ADD),
      glow: createGlowSprite(xPos, yPos, PIXI.BLEND_MODES.SCREEN),
      label: createLightLabel(xPos, yPos, id, bridgeIp),
      groupId: groupId,
      lightId: lightId,
      positionIndex: positionIndex,
      // brightness starts at 0 so new lights fade in instead of popping
      cur: { r: 0, g: 0, b: 0, bri: 0 },
      target: { r: 0, g: 0, b: 0, bri: 0 }
    };
    setLightTarget(light, hex, bri);
    light.cur.r = light.target.r;
    light.cur.g = light.target.g;
    light.cur.b = light.target.b;
    lights[bridgeIp][id] = light;
    addLightToContainer(light);
  } else {
    setLightTarget(current, hex, bri);
    // Update positions on resize
    current.halo.position.set(xPos, yPos);
    current.glow.position.set(xPos, yPos);
    current.label.position.set(xPos, yPos);
  }
}

function setLightTarget(light, hex, bri) {
  var color = parseInt(hex, 16);
  if (isNaN(color)) {
    color = 0;
  }
  light.target.r = (color >> 16) & 0xFF;
  light.target.g = (color >> 8) & 0xFF;
  light.target.b = color & 0xFF;
  light.target.bri = bri;
}

// Runs every frame: eases each light toward its target and applies tint/size
function animateLights(dtSeconds) {
  var k = 1 - Math.exp(-LERP_SPEED * dtSeconds);

  for (const values of Object.values(lights)) {
    for (const light of Object.values(values)) {
      if (light === undefined || light == null) continue;

      var cur = light.cur;
      var target = light.target;
      cur.r += (target.r - cur.r) * k;
      cur.g += (target.g - cur.g) * k;
      cur.b += (target.b - cur.b) * k;
      cur.bri += (target.bri - cur.bri) * k;

      var tint = (Math.round(cur.r) << 16) | (Math.round(cur.g) << 8) | Math.round(cur.b);
      var size = cellSize * 5 * cur.bri;

      light.glow.tint = tint;
      light.glow.width = size;
      light.glow.height = size;

      light.halo.tint = tint;
      light.halo.width = size * 2;
      light.halo.height = size * 2;
      light.halo.alpha = 0.4 * cur.bri;

      // Labels stay readable but recede when a light is off
      light.label.alpha = 0.45 + 0.55 * Math.min(cur.bri * 2, 1);
    }
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

  // Create Pixi application; render at device resolution for crisp text on high-DPI screens
  app = new PIXI.Application({
    backgroundColor: 0x11141A,
    antialias: true,
    resolution: window.devicePixelRatio || 1,
    autoResize: true
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

  // Drive the smooth light animation
  app.ticker.add(function () {
    animateLights(Math.min(app.ticker.elapsedMS / 1000, 0.1));
  });

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
    var middle = size / 2;
    var gridColor = 0x8899CC;

    // Outer border
    graphics.lineStyle(1, gridColor, 0.3);
    graphics.drawRect(0.5, 0.5, size - 1, size - 1);

    // Radar-style concentric rings around the center
    graphics.lineStyle(1, gridColor, 0.08);
    graphics.drawCircle(middle, middle, size * 0.125);
    graphics.drawCircle(middle, middle, size * 0.25);
    graphics.drawCircle(middle, middle, size * 0.375);
    graphics.drawCircle(middle, middle, size * 0.5);

    // Crosshair through the center
    graphics.lineStyle(1, gridColor, 0.14);
    graphics.moveTo(middle, 0);
    graphics.lineTo(middle, size);
    graphics.moveTo(0, middle);
    graphics.lineTo(size, middle);

    if (drawGrid) {
      graphics.lineStyle(1, gridColor, 0.15);
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

    // Center marker
    graphics.lineStyle(0);
    graphics.beginFill(gridColor, 0.35);
    graphics.drawCircle(middle, middle, 2);
    graphics.endFill();

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
    const size = Math.min(container.clientWidth, container.clientHeight);
    //previewConnection.invoke("touch", positionToXY(pos.x), positionToXY(size - pos.y)).catch(err => console.error(err.toString()));
    console.log('Pointer at: ' + positionToXY(pos.x) + ',' + positionToXY(size - pos.y));
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
        light.halo.position.set(xPos, yPos);
        light.glow.position.set(xPos, yPos);
        light.label.position.set(xPos, yPos);
        // Sizes follow the new cellSize automatically on the next ticker frame
      }
    }
  }
}

function addLightToContainer(light) {
  lightContainer.addChild(light.halo);
  lightContainer.addChild(light.glow);
  lightContainer.addChild(light.label);

  light.label.lightRef = light;

  if (allowEdit) {
    light.label.interactive = true;
    light.label.buttonMode = true;
    light.label.on('pointerdown', onDragStart)
      .on('pointerup', onDragEnd)
      .on('pointerupoutside', onDragEnd)
      .on('pointermove', onDragMove);
  }
}

function createGlowSprite(x, y, blendMode) {
  var glowRing = PIXI.Sprite.fromImage("../glow.png");
  glowRing.anchor.set(0.5, 0.5);
  glowRing.height = 0;
  glowRing.width = 0;
  glowRing.position.x = x;
  glowRing.position.y = y;
  glowRing.blendMode = blendMode;
  return glowRing;
}

function createLightLabel(x, y, id, bridgeIp) {
  var textColor = 0xE9ECEF;
  if (allowEdit)
    textColor = 0xFF6666;

  var label = new PIXI.Text(String(id), {
    fontFamily: 'Arial',
    fontSize: 12,
    fill: textColor,
    dropShadow: true,
    dropShadowColor: 0x000000,
    dropShadowBlur: 3,
    dropShadowDistance: 1,
    dropShadowAlpha: 0.8
  });
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
    if (this.lightRef) {
      this.lightRef.glow.position.set(newPosition.x, newPosition.y);
      this.lightRef.halo.position.set(newPosition.x, newPosition.y);
    }
  }
}
