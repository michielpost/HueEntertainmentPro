
import * as THREE from './three.module.min.js';

//import Stats from 'three/addons/libs/stats.module.js';
//import { GUI } from 'three/addons/libs/lil-gui.module.min.js';

import { OrbitControls } from './OrbitControls.js';

import { FontLoader } from './FontLoader.js';
import { TextGeometry } from './TextGeometry.js';

let camera, scene, renderer, hemiLight, stats;
let floorMat;

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
  shadows: true,
  exposure: 1,
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


  camera = new THREE.PerspectiveCamera(50, window.innerWidth / window.innerHeight, 0.1, 100);
  // Adjust PerspectiveCamera to start from a half top-down, back-to-front view
  camera.position.set(0, 1.5, 16); // Half top-down view
  camera.lookAt(-3, 0, 0); // Focus on the exact middle

  scene = new THREE.Scene();

  hemiLight = new THREE.HemisphereLight(0xddeeff, 0x0f0e0d, 0.02);
  scene.add(hemiLight);



  floorMat = new THREE.MeshStandardMaterial({
    roughness: 0.8,
    color: 0xffffff,
    metalness: 0.2,
    bumpScale: 1
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

    const textMaterial = new THREE.MeshStandardMaterial({ color: 0xffaa00 });
    const textMesh = new THREE.Mesh(textGeometry, textMaterial);

    // Position so it sits nicely on your floorMesh
    textGeometry.computeBoundingBox();
    const centerOffset = -0.5 * (textGeometry.boundingBox.max.x - textGeometry.boundingBox.min.x);
    textMesh.position.set(centerOffset, 0, -7.5); // adjust Y so it's just above the plane
    textMesh.scale.set(1, 1, 0.001); // shrink 20% of original size
    scene.add(textMesh);
  });


  const floorGeometry = new THREE.PlaneGeometry(15, 15);
  const floorMesh = new THREE.Mesh(floorGeometry, floorMat);
  //floorMesh.receiveShadow = true;
  floorMesh.rotation.x = - Math.PI / 2.0;
  floorMesh.position.y = -3; // Move the floor slightly down
  scene.add(floorMesh);

  const backGeometry = new THREE.PlaneGeometry(15, 7.5);
  const backMesh = new THREE.Mesh(backGeometry, floorMat);
  //backMesh.receiveShadow = true;
  //leftSideMesh.rotation.x = - Math.PI / 2.0;
  backMesh.position.y = 0.75; // Move the floor slightly down
  backMesh.position.z = -7.5; // Move the floor slightly down
  scene.add(backMesh);

  const rightSideGeometry = new THREE.PlaneGeometry(15, 7.5);
  const rightSideMesh = new THREE.Mesh(rightSideGeometry, floorMat);
  //rightSideMesh.receiveShadow = true;
  rightSideMesh.rotation.y = - Math.PI / 2.0;
  rightSideMesh.position.y = 0.75; // Move the floor slightly down
  rightSideMesh.position.x = 7.5; // Move the floor slightly down
  scene.add(rightSideMesh);

  const leftSideGeometry = new THREE.PlaneGeometry(15, 7.5);
  const leftSideMesh = new THREE.Mesh(leftSideGeometry, floorMat);
  //leftSideMesh.receiveShadow = true;
  leftSideMesh.rotation.y = Math.PI / 2.0;
  leftSideMesh.position.y = 0.75; // Move the floor slightly down
  leftSideMesh.position.x = -7.5; // Move the floor slightly down
  scene.add(leftSideMesh);

  renderer = new THREE.WebGLRenderer();
  renderer.setPixelRatio(window.devicePixelRatio);
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.setAnimationLoop(animate);
  //renderer.shadowMap.enabled = true;
  renderer.toneMapping = THREE.ReinhardToneMapping;
  container.appendChild(renderer.domElement);


  const controls = new OrbitControls(camera, renderer.domElement);
  controls.minDistance = 1;
  controls.maxDistance = 35;

  window.addEventListener('resize', onWindowResize);


  // const gui = new GUI();

  // gui.add( params, 'hemiIrradiance', Object.keys( hemiLuminousIrradiances ) );
  // gui.add( params, 'bulbPower', Object.keys( bulbLuminousPowers ) );
  // gui.add( params, 'exposure', 0, 1 );
  // gui.add( params, 'shadows' );
  // gui.open();

}

function onWindowResize() {

  camera.aspect = window.innerWidth / window.innerHeight;
  camera.updateProjectionMatrix();

  renderer.setSize(window.innerWidth, window.innerHeight);

}

//

function animate() {

  renderer.toneMappingExposure = Math.pow(params.exposure, 5.0); // to allow for very bright scenes.
  renderer.shadowMap.enabled = params.shadows;

  // if ( params.shadows !== previousShadowMap ) {

  //     ballMat.needsUpdate = true;
  //     cubeMat.needsUpdate = true;
  //     floorMat.needsUpdate = true;
  //     previousShadowMap = params.shadows;
  // }

  hemiLight.intensity = hemiLuminousIrradiances[params.hemiIrradiance];

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

    if (existingLight) {
      // Update existing
      existingLight.position.set(light.x * 5, light.z * 2, light.y * -5);
      existingLight.color.setHex(color);
      existingLight.intensity = light.bri * 0.4;
      existingLight.power = light.bri * 400;

      if (existingLight.children[0]) {
        existingLight.children[0].material.color.setHex(color);
        existingLight.children[0].material.emissive.setHex(color);
        existingLight.children[0].geometry.dispose(); // free old geometry
        existingLight.children[0].geometry = new THREE.SphereGeometry((light.bri + 0.1) * 0.35, 16, 8);
      }
    } else {
      // Create new
      const newLight = createLight(color, [light.x * 5, light.z * 2, light.y * -5], light.bri);
      newLight.color.setHex(color);
      newLight.intensity = light.bri * 0.4;
      newLight.power = light.bri * 400;

      newLight.userData.id = light.id; // <-- store consistent lowercase
      scene.add(newLight);
      lightMap.set(light.bridge + light.id, newLight);
    }
  }

}

// Move createLight to global scope
function createLight(color, position, irradiance = 1) {
  const size = Math.sqrt(irradiance) * 0.35; // Scale size based on irradiance
  const light = new THREE.PointLight(color, 1, 100, 2);
  const geometry = new THREE.SphereGeometry(size, 16, 8);
  const material = new THREE.MeshStandardMaterial({
    emissive: color,
    emissiveIntensity: 1,
    color: 0xffffff // Ensure the bulb is visible with white base color
  });
  const bulbMesh = new THREE.Mesh(geometry, material); // Create bulb mesh
  light.add(bulbMesh); // Add bulb mesh to light
  light.position.set(...position);
  //light.castShadow = true;
  scene.add(light);
  return light;
}
