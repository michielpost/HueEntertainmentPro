
import * as THREE from './three.module.min.js';

//import Stats from 'three/addons/libs/stats.module.js';
//import { GUI } from 'three/addons/libs/lil-gui.module.min.js';

import { OrbitControls } from './OrbitControls.js';

import { FontLoader } from './FontLoader.js';
import { TextGeometry } from './TextGeometry.js';

let camera, scene, renderer, hemiLight, controls, clock, stats;
let floorMat, wallMat, baseboardMat;
let textMesh;
let dustNear, dustFar;
let glowTexture;
let sharedBulbGeo, sharedBeamGeo, sharedSplashGeo;

const FLOOR_Y = -3;
const ROOM_SIZE = 15;

// Scratch colors reused every frame to avoid per-frame allocations
const _avgColor = new THREE.Color();
const _skyBase = new THREE.Color(0xddeeff);
const _baseboardBase = new THREE.Color(0x453a7a);

// ref for lumens: http://www.power-sure.com/lumens.htm
const bulbLuminousPowers = {
  '110000 lm (1000W)': 110000,
  '3500 lm (300W)': 3500,
  '1700 lm (100W)': 1700,
  '800 lm (60W)': 800,
  '400 lm (40W)': 400,
  '180 lm (25W)': 180,
  '20 lm (4W)': 20,
  'Off': 0
};

// ref for solar irradiances: https://en.wikipedia.org/wiki/Lux
const hemiLuminousIrradiances = {
  '0.0001 lx (Moonless Night)': 0.0001,
  '0.002 lx (Night Airglow)': 0.002,
  '0.5 lx (Full Moon)': 0.5,
  '3.4 lx (City Twilight)': 3.4,
  '50 lx (Living Room)': 50,
  '100 lx (Very Overcast)': 100,
  '350 lx (Office Room)': 350,
  '400 lx (Sunrise/Sunset)': 400,
  '1000 lx (Overcast)': 1000,
  '18000 lx (Daylight)': 18000,
  '50000 lx (Direct Sun)': 50000
};

const params = {
  shadows: false,
  exposure: 1.2,
  bulbPower: Object.keys(bulbLuminousPowers)[4],
  hemiIrradiance: Object.keys(hemiLuminousIrradiances)[2]
};

export function renderPreviewGrid() {

  const container = document.getElementById('container');

  // Clean up any previous preview session, so lights of a previous connection don't linger
  if (renderer) {
    renderer.setAnimationLoop(null);
    renderer.dispose();
    renderer.domElement.remove();
    renderer = null;
  }
  lightMap.clear();
  pendingLightData = null;

  //stats = new Stats();
  //container.appendChild( stats.dom );

  clock = new THREE.Clock();

  camera = new THREE.PerspectiveCamera(50, window.innerWidth / window.innerHeight, 0.1, 100);
  // Adjust PerspectiveCamera to start from a half top-down, back-to-front view
  camera.position.set(0, 1.5, 16); // Half top-down view
  camera.lookAt(-3, 0, 0); // Focus on the exact middle

  scene = new THREE.Scene();
  scene.background = new THREE.Color(0x000005);
  scene.fog = new THREE.FogExp2(0x000008, 0.015);

  hemiLight = new THREE.HemisphereLight(0xddeeff, 0x0f0e0d, 0.02);
  scene.add(hemiLight);

  // Dark glossy floor so the light colors pop and reflect as highlights
  floorMat = new THREE.MeshStandardMaterial({
    roughness: 0.25,
    color: 0x16161a,
    metalness: 0.6
  });

  // Slightly lighter matte walls so color washes stay clearly visible
  wallMat = new THREE.MeshStandardMaterial({
    roughness: 0.7,
    color: 0x2f2f33,
    metalness: 0.1
  });

  const loader = new FontLoader();
  loader.load('https://threejs.org/examples/fonts/helvetiker_regular.typeface.json', (font) => {

    const textGeometry = new TextGeometry('Hue Entertainment Pro', {
      font: font,
      size: 1,          // size of the letters
      height: 0.05,      // extrusion depth
      curveSegments: 1,
      bevelEnabled: true,
      bevelThickness: 0.02,
      bevelSize: 0.02,
      bevelSegments: 3
    });

    const textMaterial = new THREE.MeshStandardMaterial({
      color: 0x1a0e00,
      emissive: 0xffaa00,
      emissiveIntensity: 0.9
    });
    textMesh = new THREE.Mesh(textGeometry, textMaterial);

    // Position so it sits nicely against the back wall
    textGeometry.computeBoundingBox();
    const centerOffset = -0.5 * (textGeometry.boundingBox.max.x - textGeometry.boundingBox.min.x);
    textMesh.position.set(centerOffset, 0, -7.45); // just in front of the back wall
    textMesh.scale.set(1, 1, 0.001);
    scene.add(textMesh);
  });


  const floorGeometry = new THREE.PlaneGeometry(ROOM_SIZE, ROOM_SIZE);
  const floorMesh = new THREE.Mesh(floorGeometry, floorMat);
  floorMesh.rotation.x = - Math.PI / 2.0;
  floorMesh.position.y = FLOOR_Y;
  scene.add(floorMesh);

  // Neon grid overlay on the dance floor
  const grid = new THREE.GridHelper(ROOM_SIZE, 30, 0x6644cc, 0x221b3a);
  grid.position.y = FLOOR_Y + 0.01;
  grid.material.transparent = true;
  grid.material.opacity = 0.5;
  grid.material.depthWrite = false;
  scene.add(grid);

  const backGeometry = new THREE.PlaneGeometry(ROOM_SIZE, 7.5);
  const backMesh = new THREE.Mesh(backGeometry, wallMat);
  backMesh.position.y = 0.75;
  backMesh.position.z = -7.5;
  scene.add(backMesh);

  const rightSideGeometry = new THREE.PlaneGeometry(ROOM_SIZE, 7.5);
  const rightSideMesh = new THREE.Mesh(rightSideGeometry, wallMat);
  rightSideMesh.rotation.y = - Math.PI / 2.0;
  rightSideMesh.position.y = 0.75;
  rightSideMesh.position.x = 7.5;
  scene.add(rightSideMesh);

  const leftSideGeometry = new THREE.PlaneGeometry(ROOM_SIZE, 7.5);
  const leftSideMesh = new THREE.Mesh(leftSideGeometry, wallMat);
  leftSideMesh.rotation.y = Math.PI / 2.0;
  leftSideMesh.position.y = 0.75;
  leftSideMesh.position.x = -7.5;
  scene.add(leftSideMesh);

  glowTexture = createGlowTexture();

  // Geometry shared by every light fixture
  sharedBulbGeo = new THREE.SphereGeometry(0.35, 24, 12);
  sharedBeamGeo = new THREE.ConeGeometry(1, 1, 24, 1, true);
  sharedBeamGeo.translate(0, -0.5, 0); // apex at origin, base at y = -1
  sharedSplashGeo = new THREE.PlaneGeometry(1, 1);

  // Neon baseboard strips along the floor edges; tinted by the lights in animate()
  baseboardMat = new THREE.MeshBasicMaterial({ color: _baseboardBase });
  const stripGeo = new THREE.BoxGeometry(ROOM_SIZE, 0.06, 0.06);
  for (const [x, z, rotY] of [
    [0, -7.5, 0],
    [0, 7.5, 0],
    [-7.5, 0, Math.PI / 2],
    [7.5, 0, Math.PI / 2]
  ]) {
    const strip = new THREE.Mesh(stripGeo, baseboardMat);
    strip.position.set(x, FLOOR_Y + 0.03, z);
    strip.rotation.y = rotY;
    scene.add(strip);
  }

  // Floating dust particles that catch the atmosphere
  dustNear = createDust(250, 0.05, 0.35);
  dustFar = createDust(180, 0.03, 0.2);
  scene.add(dustNear);
  scene.add(dustFar);

  renderer = new THREE.WebGLRenderer({ antialias: true });
  renderer.setPixelRatio(window.devicePixelRatio);
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.setAnimationLoop(animate);
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = params.exposure;
  container.appendChild(renderer.domElement);


  controls = new OrbitControls(camera, renderer.domElement);
  controls.minDistance = 1;
  controls.maxDistance = 35;
  controls.enableDamping = true;
  controls.dampingFactor = 0.05;
  controls.target.set(0, 0, 0);

  window.addEventListener('resize', onWindowResize);


  // const gui = new GUI();

  // gui.add( params, 'hemiIrradiance', Object.keys( hemiLuminousIrradiances ) );
  // gui.add( params, 'bulbPower', Object.keys( bulbLuminousPowers ) );
  // gui.add( params, 'exposure', 0, 1 );
  // gui.add( params, 'shadows' );
  // gui.open();

}

function createGlowTexture() {
  const size = 128;
  const canvas = document.createElement('canvas');
  canvas.width = size;
  canvas.height = size;
  const ctx = canvas.getContext('2d');
  const gradient = ctx.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
  gradient.addColorStop(0, 'rgba(255,255,255,1)');
  gradient.addColorStop(0.2, 'rgba(255,255,255,0.55)');
  gradient.addColorStop(0.5, 'rgba(255,255,255,0.15)');
  gradient.addColorStop(1, 'rgba(255,255,255,0)');
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, size, size);
  return new THREE.CanvasTexture(canvas);
}

function createDust(count, size, opacity) {
  const positions = new Float32Array(count * 3);
  for (let i = 0; i < count; i++) {
    positions[i * 3] = (Math.random() - 0.5) * ROOM_SIZE;
    positions[i * 3 + 1] = FLOOR_Y + Math.random() * 8;
    positions[i * 3 + 2] = (Math.random() - 0.5) * ROOM_SIZE;
  }
  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
  const material = new THREE.PointsMaterial({
    color: 0x8899cc,
    size: size,
    map: glowTexture,
    transparent: true,
    opacity: opacity,
    blending: THREE.AdditiveBlending,
    depthWrite: false
  });
  return new THREE.Points(geometry, material);
}

function onWindowResize() {

  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();

  renderer.setSize(window.innerWidth, window.innerHeight);

}

//

function animate() {

  const dt = Math.min(clock.getDelta(), 0.1);
  const t = clock.elapsedTime;
  // Exponential smoothing factor: frame-rate independent easing toward targets
  const k = 1 - Math.exp(-12 * dt);

  // Push any throttled light data even when no new updates arrive
  if (pendingLightData && performance.now() - lastUpdate > updateInterval) {
    flushLightUpdate();
  }

  _avgColor.setRGB(0, 0, 0);
  let avgBri = 0;

  for (const light of lightMap.values()) {
    const target = light.userData.target;
    const cur = light.userData.cur;

    light.position.lerp(target.position, k);
    cur.color.lerp(target.color, k);
    cur.bri += (target.bri - cur.bri) * k;

    applyLightState(light, cur);

    _avgColor.add(cur.color);
    avgBri += cur.bri;
  }

  if (lightMap.size > 0) {
    _avgColor.multiplyScalar(1 / lightMap.size);
    avgBri /= lightMap.size;
  }

  // Ambient bounce: the room's ambience follows the overall light color/energy
  hemiLight.color.lerpColors(_skyBase, _avgColor, 0.6);
  hemiLight.intensity = hemiLuminousIrradiances[params.hemiIrradiance] + avgBri * 1.5;

  // Baseboards pick up the dominant color and breathe with the energy
  baseboardMat.color.lerpColors(_baseboardBase, _avgColor, 0.45);
  baseboardMat.color.multiplyScalar(0.5 + avgBri * 0.5);

  // Slow drifting, faintly twinkling dust for atmosphere
  if (dustNear) {
    dustNear.rotation.y = t * 0.015;
    dustFar.rotation.y = t * -0.01;
    dustNear.position.y = Math.sin(t * 0.25) * 0.15;
    dustFar.position.y = Math.cos(t * 0.2) * 0.1;
    dustNear.material.opacity = 0.35 * (0.8 + 0.2 * Math.sin(t * 0.7));
    dustFar.material.opacity = 0.2 * (0.8 + 0.2 * Math.cos(t * 0.55));
  }

  // Gentle neon pulse on the title
  if (textMesh) {
    textMesh.material.emissiveIntensity = 0.75 + 0.35 * Math.sin(t * 1.5);
  }

  controls.update();

  renderer.render(scene, camera);

  //stats.update();

}

const lightMap = new Map(); // Id -> light

let lastUpdate = 0;
let pendingLightData = null;
const updateInterval = 100; // ms

export function scheduleLightUpdate(newLightData) {
  pendingLightData = newLightData;

  const now = performance.now();
  if (now - lastUpdate > updateInterval) {
    flushLightUpdate();
  }
}

function flushLightUpdate() {
  if (pendingLightData) {
    updateLights(pendingLightData);
    pendingLightData = null;
    lastUpdate = performance.now();
  }
}

export function updateLights(newLightData) {

  for (const light of newLightData) {
    let color = parseInt(light.hex, 16);
    let existingLight = lightMap.get(light.bridge + light.id);

    if (light.bri == 0) {
      color = 0x000000;
    }

    const position = new THREE.Vector3(light.x * 5, light.z * 2, light.y * -5);

    if (existingLight) {
      // Only update the target; the animation loop eases toward it
      existingLight.userData.target.position.copy(position);
      existingLight.userData.target.color.setHex(color);
      existingLight.userData.target.bri = light.bri;
    } else {
      const newLight = createLight(color, position, light.bri);
      newLight.userData.id = light.id; // <-- store consistent lowercase
      scene.add(newLight);
      lightMap.set(light.bridge + light.id, newLight);
    }
  }

}

// Applies the current (eased) color/brightness state to all parts of a light
function applyLightState(light, cur) {
  const bri = Math.max(cur.bri, 0);

  light.color.copy(cur.color);
  light.intensity = bri * 55; // point light punch into the dark room

  const bulb = light.userData.bulb;
  bulb.material.color.copy(cur.color);
  bulb.material.emissive.copy(cur.color);
  bulb.material.emissiveIntensity = 1 + bri * 2;
  const bulbScale = 0.15 + bri;
  bulb.scale.set(bulbScale, bulbScale, bulbScale);

  const glow = light.userData.glow;
  glow.material.color.copy(cur.color);
  glow.material.opacity = Math.min(bri * 1.2, 1);
  const glowScale = 1 + bri * 4;
  glow.scale.set(glowScale, glowScale, 1);

  const beamHeight = Math.max(light.position.y - FLOOR_Y, 0.01);
  const beamSpread = 1 + bri * 1.2;

  const beam = light.userData.beam;
  beam.material.color.copy(cur.color);
  beam.material.opacity = bri * 0.13;
  beam.scale.set(beamSpread, beamHeight, beamSpread);

  const beamInner = light.userData.beamInner;
  beamInner.material.color.copy(cur.color);
  beamInner.material.opacity = bri * 0.22;
  beamInner.scale.set(beamSpread * 0.4, beamHeight, beamSpread * 0.4);

  const splash = light.userData.splash;
  splash.material.color.copy(cur.color);
  splash.material.opacity = bri * 0.4;
  splash.position.set(light.position.x, FLOOR_Y + 0.02, light.position.z);
  const splashScale = 2 + bri * 4;
  splash.scale.set(splashScale, splashScale, 1);
}

// Creates a light fixture: point light + emissive bulb + glow sprite + volumetric beam + floor splash
function createLight(color, position, bri = 1) {
  const light = new THREE.PointLight(color, 1, 100, 2);
  light.position.copy(position);

  const bulbMaterial = new THREE.MeshStandardMaterial({
    emissive: color,
    emissiveIntensity: 1,
    color: 0xffffff
  });
  const bulbMesh = new THREE.Mesh(sharedBulbGeo, bulbMaterial);
  light.add(bulbMesh);

  const glowMaterial = new THREE.SpriteMaterial({
    map: glowTexture,
    color: color,
    transparent: true,
    opacity: 1,
    blending: THREE.AdditiveBlending,
    depthWrite: false
  });
  const glowSprite = new THREE.Sprite(glowMaterial);
  light.add(glowSprite);

  // Open-ended cone with its apex at the light, flaring down to the floor;
  // a wide faint outer cone plus a narrow brighter core for volumetric depth
  const beamMaterial = new THREE.MeshBasicMaterial({
    color: color,
    transparent: true,
    opacity: 0.1,
    blending: THREE.AdditiveBlending,
    side: THREE.DoubleSide,
    depthWrite: false,
    fog: false
  });
  const beamMesh = new THREE.Mesh(sharedBeamGeo, beamMaterial);
  light.add(beamMesh);

  const beamInnerMesh = new THREE.Mesh(sharedBeamGeo, beamMaterial.clone());
  light.add(beamInnerMesh);

  // Color pool on the floor where the beam lands (world space, not a child)
  const splashMaterial = new THREE.MeshBasicMaterial({
    map: glowTexture,
    color: color,
    transparent: true,
    opacity: 0,
    blending: THREE.AdditiveBlending,
    depthWrite: false
  });
  const splashMesh = new THREE.Mesh(sharedSplashGeo, splashMaterial);
  splashMesh.rotation.x = - Math.PI / 2.0;
  scene.add(splashMesh);

  light.userData.bulb = bulbMesh;
  light.userData.glow = glowSprite;
  light.userData.beam = beamMesh;
  light.userData.beamInner = beamInnerMesh;
  light.userData.splash = splashMesh;
  light.userData.target = {
    position: position.clone(),
    color: new THREE.Color(color),
    bri: bri
  };
  light.userData.cur = {
    color: new THREE.Color(color),
    bri: bri
  };

  applyLightState(light, light.userData.cur);

  return light;
}
