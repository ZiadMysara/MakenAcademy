import { Routes } from '@angular/router';

export const examsRoutes: Routes = [
  {path:'',loadComponent:()=>import('./exam-list/exam-list.component').then(m=>m.ExamListComponent)},
  {path:'new',loadComponent:()=>import('./exam-form/exam-form.component').then(m=>m.ExamFormComponent)},
  {path:':id/edit',loadComponent:()=>import('./exam-form/exam-form.component').then(m=>m.ExamFormComponent)}
];
